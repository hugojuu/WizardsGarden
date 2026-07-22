# 진행 상태

**현재**: S03 완료 — 다음 세션은 **S04**

## 완료된 세션
- **S03 — 정원 구역** (2026-07-22)
  - 순수 C# 코어 (`Assets/Scripts/Core`): `GardenSlot`(plantId + 심은 시점 자원초만 저장 — 진행도·단계·수확 판정은 현재 자원초에서 파생 계산) · `Garden`(초기 4칸, TryPlant/TryHarvest, 세이브 복원 시 슬롯 수 = max(4, 저장 수)로 S04 확장 호환) · `Inventory`(id→수량, SortedDictionary로 나열·세이브 순서 결정적, Changed 이벤트) · `GrowthStage`+유틸(새싹<50%<성장<100%≤완료) · `GameSession`에 Garden/Inventory 소유 + `TryPlant`/`TryHarvestToInventory` 편의 메서드(수확→인벤 적재 규칙도 코어에)
  - 세이브 v2: `SaveData.CurrentVersion` 1→2, `gardenSlots`(빈 슬롯 plantId="")·`inventoryItems` 추가, `SaveMigrator` case 1(v1→v2: 빈 밭·빈 인벤 초기화). 로드는 상태 보존만 — 오프라인 정산 없음(S08)
  - 어댑터: `GardenScreen`(`Assets/Scripts`, SampleScene 배치) — uGUI를 코드로 생성(시계 라벨·2×2 슬롯 그리드·종자 선택 패널·수확물 패널), 매 프레임 현재 상태 폴링(이벤트 미구독 — S02 인계 방침), 빈 슬롯 클릭→종자 패널→심기 / 완료 슬롯 클릭→수확. `PlaceholderPalette`(원소→색 규약). 스모크 테스트용 공개 API `TryPlantSeed`/`TryHarvestSlot`/`OnSlotClicked`
  - 에디터: 메뉴 **WizardGarden > Setup Garden Scene (S03)** — SampleScene에 GardenScreen 배치 + 티어1 종자 4종 SO 참조 연결 + Build Settings 등록 (재실행 안전)
  - 검증: 컴파일 에러/경고 0 · EditMode 테스트 **65/65 통과**(기존 40 + S03 신규 25: Garden 12 · Inventory 6 · SaveMigrationV2 7) · 플레이 모드 스모크로 4종 심기→시간 가속·실시간 3초 성장→수확→인벤토리 적재·세이브 JSON v2 필드·종자 패널 버튼 흐름·완료 슬롯 클릭 수확 전부 실제 확인 (스크린샷 `Captures/S03_garden_smoke.png` — git 미추적, 검토 후 삭제 가능). 스모크가 만든 세이브는 삭제로 원상 복구
  - 완료 기준 대비: 4종을 심고 수확해 인벤토리에 쌓임 ✅ (스모크에서 4슬롯 전부) · 성장 시간이 데이터 값대로 작동 ✅ (티어1 3초 — 조기 수확 거부 + 실시간 3초 후 완료 확인, 경계값은 EditMode 테스트)
- **S02 — 세이브/로드 + 게임 시계** (2026-07-22)
  - 순수 C# 코어 (`Assets/Scripts/Core`, ns `WizardGarden.Core`): `GameClock`(이중 트랙: EventSeconds/ResourceSeconds, 배속 TimeScale, TimeOfDayChanged·DayChanged 이벤트, SkipGameHours, AddResourceSeconds) · `TimeOfDayUtility`(시각→구간) · `SaveData`+`SaveMigrator`(버전 필드, 마이그레이션 체인 자리) · `SaveRepository`(JSON 파일 IO, 임시파일→교체 쓰기, 손상/미래버전 로드 거부) · `GameSession`(로드→복원→오프라인 raw 초 계산 / 저장→UTC 시각 기록) · `IUtcClock`/`SystemUtcClock`(시간 주입)
  - 어댑터: `GameClockRunner`(MonoBehaviour, `Assets/Scripts`, ns `WizardGarden`) — 플레이 시작 시 자동 생성(`RuntimeInitializeOnLoadMethod`, 씬 배치 불필요), 매 프레임 `Time.unscaledDeltaTime` tick, 60초 자동 저장 + 종료 시 저장
  - 에디터 치트: 메뉴 **WizardGarden > Time Cheat (S02)** — 배속 프리셋 x1/x10/x60/x360/x900(=현실 1초에 하루)·슬라이더, +1시간/+1일/+7일 스킵, 즉시 저장, 세이브 삭제, 시계 상태 실시간 표시
  - 검증: 컴파일 에러/경고 0 · EditMode 테스트 **40/40 통과**(S01의 6 + S02 신규 34: TimeOfDayUtility 11 · GameClock 11 · SaveRepository 7 · GameSession 5) · `execute_code` 스모크 테스트로 실제 persistentDataPath에 save.json 생성→로드 라운드트립·오프라인 초 계산 확인 후 원상 복구
  - 완료 기준 대비: 껐다 켜면 상태 복원 ✅ (GameSessionTests.SaveQuitRestart + 스모크) · 오프라인 경과 초 정확 ✅ (가짜 시계로 3600초 검증, 시계 역행은 0 클램프) · 배속으로 게임 내 하루 순환 ✅ (x900 테스트: 아침→낮→저녁→야간→아침 전환 4회)
- **S01 — 프로젝트 구조 + 데이터 스키마** (2026-07-21)
  - 폴더: `Assets/Scripts`(asmdef `WizardGarden`) / `Scripts/Core` / `Scripts/Data` / `Scripts/Editor`(asmdef `WizardGarden.Editor`) / `Assets/Data` / `Assets/Art/Placeholders` / `Assets/Tests`(asmdef `WizardGarden.Tests`, EditMode)
  - `ElementComposition` struct: 5칸(불/물/대지/바람/별⭐예약) + 합산(`+`)·일치(`Matches`)·포함(`Contains`)·최다 원소(`TryGetDominantElement` — 동률·전부0이면 false, 실패 부산물 분기용) + `Element` 인덱서·`Total`
  - enum: `Element`(5) · `Season`(4) · `Weather`(6: 맑음/비/폭풍우/달빛밤/유성우/일식) · `Rarity`(4) · `Job`(3) · `TimeOfDay`(4: 아침06-09/낮09-18/저녁18-21/야간21-06) · `PotionCategory`(5: 공격/회복/방어/보조/특수)
  - SO: `ItemData`(공통 베이스: id·displayName·composition·baseValue) ← `PlantData`(tier·growthSeconds) / `MaterialData`(processingStage 1~3) / `PotionData`(category·requiredIngredients·conditionTags·equipEffectId). `ApprenticeData`(job·rarity·4스탯·passiveIds)
  - 테스트: `ElementCompositionTests` (연산 6케이스, EditMode)
  - 완료 기준 대비: 폴더/스키마/컨벤션 기록 ✅ · 컴파일 클린 ✅ (dotnet 교차 빌드 0에러/0경고 + Editor.log에서 Unity 컴파일 성공 확인) · 샘플 SO 에셋 ✅ (2026-07-22 MCP 연결 후 부트스트랩 실행 — 5종 생성·값 검증·EditMode 테스트 6/6 통과)

## 세션 간 인계 메모
- 2026-07-21 (기획 세션): 기획서 확정 — 완성형 모델(출시 = 완결), 포션 33종(30 + 실험 일지 3), 출시 로스터 22명/보너스 6명, 마일스톤 M0~M8, 세션 분할 20개. Unity 프로젝트는 URP 2D 템플릿 초기 상태 (코드 없음, 기본 씬만 존재).
- 2026-07-21 (S01): MCP 브리지 미연결로 에디터 원격 조작 불가 → **2026-07-22 해소**: MCP 연결 후 부트스트랩 실행 완료. 샘플 SO 5종 생성 확인 (`Assets/Data/Plants` 4종 + `Assets/Data/Potions` 1종, 값 기획서와 일치), 콘솔 에러 0, EditMode 테스트 6/6 통과. 부트스트랩 메뉴(**WizardGarden > Create Sample Data (S01)**)는 재실행 안전 — 이후 세션에서 데이터 리셋용으로 재사용 가능
- 패시브는 현재 `ApprenticeData.passiveIds`(문자열 ID 목록)로만 참조 — 패시브 데이터 테이블(SO)은 해당 콘텐츠 세션에서 추가
- (S02→이후 세션) 시계 구독 방법: `GameClockRunner.Instance.Clock`의 `TimeOfDayChanged(이전, 현재)`·`DayChanged(새 일차)` 이벤트 구독. 큰 배속으로 구간을 건너뛰면 중간 구간 이벤트는 생략되므로, 구독자는 이벤트 인자보다 `CurrentTimeOfDay` 현재 상태를 기준으로 동작할 것
- (S02→S08) 오프라인 정산 인터페이스: 시작 시 `GameSession.PendingOfflineSeconds`(raw 초 — 캡·효율 미적용)를 읽어 정산하고, 정산분은 `GameClock.AddResourceSeconds`(자원 트랙 전용 훅)로 반영한 뒤 `ClearPendingOfflineSeconds()` 호출. S02는 "몇 초 지났는지"만 제공
- (S02→S11) 계절은 사건 시간 위에 얹으면 됨: `GameClock.EventSeconds` 또는 `DayIndex`(1일=900초, 7일=1계절) 기반. 검증은 Time Cheat 창의 +7일 스킵/배속 사용
- (S02) 세이브 스키마 확장 시: `SaveData`에 필드 추가 + `CurrentVersion` +1 + `SaveMigrator.TryMigrate`에 이전 버전 → 새 버전 변환 case 추가
- (S03→S04) 인벤토리 사용법: `GameClockRunner.Instance.Session.Inventory` — `Add(id, n)`/`GetCount(id)`/`Entries`(id 오름차순)/`Changed` 이벤트. **제거(판매·가공 소비용 Remove)는 아직 없음 — S04에서 추가할 것**
- (S03→S04) 밭 슬롯 확장: `Garden` 생성자가 slotCount를 받고 `RestoreFrom`이 max(4, 저장된 슬롯 수)로 복원하므로, S04는 확장 구매 시 슬롯을 늘려 저장하면 됨 (슬롯 수 = `gardenSlots` 목록 길이, 별도 카운트 필드 없음). `GardenScreen`의 그리드는 현재 `Garden.SlotCount`만큼 시작 시 1회 생성 — 런타임 확장 시 위젯 재생성 필요
- (S03→S04) 심기 가능한 종자 목록·growthSeconds 조회는 `GardenScreen.seedOptions`(인스펙터 SO 참조)가 유일한 소스. 세이브에 있는데 목록에 없는 식물 id는 경고 후 즉시 수확 가능 처리(세이브 잠김 방지)

## 개발 중 바뀐 결정
- (S01) `ElementComposition`은 "5칸 배열" 의미를 유지하되 **int[] 대신 고정 필드 5개 + `Element` 인덱서**로 구현 — struct 안의 배열은 참조 공유(값 의미론 파괴) 문제가 있어 값 타입 안전성과 인스펙터 편의를 위함. 슬롯 수 상수 `ElementComposition.SlotCount = 5`
- (S01) 식물·재료·포션은 공통 베이스 `ItemData` 상속 (기획서 3장 "모든 식물·재료·포션은 원소 조성을 가짐"의 코드 반영). 포션 지정 재료는 `ItemData` 참조로 식물·가공재료 모두 지정 가능
- (S01) 포션 조건(야간/날씨 등)은 enum이 아닌 **문자열 태그 목록**(`conditionTags`)으로 저장 — 조건 종류 추가를 데이터 작업으로 유지, 해석은 S05 매칭 알고리즘에서
- (S02) 시간·세이브 **순수 로직도 `Assets/Scripts/Core`/`WizardGarden.Core`에 배치** — CLAUDE.md의 S01 확정 네임스페이스 4종을 유지하기 위해 새 네임스페이스(예: Systems)를 만들지 않고 Core를 "공용 타입 + 순수 로직"으로 해석. MonoBehaviour 어댑터만 `Assets/Scripts` 루트(ns `WizardGarden`). 폴더 설명 문구 갱신 여부는 오케스트레이터 판단에 위임
- (S02) 새 게임 시작 시각 = **1일차 06:00**(아침 구간 시작) — 시작 직후 아침 모험 출발 윈도우를 온전히 경험하게 하기 위함. 상수 `GameClock.StartHourOfDay`
- (S02) `SaveData.version` 필드 기본값은 **0**(CurrentVersion 아님) — 버전 필드 없는/손상 JSON을 구버전으로 식별해 로드 거부하기 위함. 새 세이브는 반드시 `SaveData.CreateNew()`로 생성
- (S02) 배속 치트는 Unity `Time.timeScale`이 아니라 **`GameClock.TimeScale`**(시계 내부 배속) — 물리·애니메이션에 영향 없이 게임 시간만 가속. Runner는 `Time.unscaledDeltaTime`으로 tick
- (S03) **스키마 변경**: `PlantData.displayEmoji`(string, 기본 "🌱") 추가 — 플레이스홀더 표시용, UI는 이 필드 참조로만 표시 (아트 스왑 시 이 지점만 교체). 샘플 4종은 원소 이모지(🔥💧🌍💨)로 설정
- (S03) 성장은 **심은 시점(자원초) 저장 + 파생 계산** — growthSeconds를 세이브에 저장하지 않아 데이터 밸런스 변경이 기존 세이브에 즉시 적용되고, S08 오프라인 정산은 `AddResourceSeconds`만 하면 성장에 자동 반영됨. 슬롯 상태 갱신은 이벤트 구독 없이 매 프레임 현재 상태 폴링
- (S03) 씬은 **SampleScene 재사용** (새 Garden 씬 만들지 않음 — 카메라·라이트 기존 것 활용, 씬 구성은 `GardenSceneBootstrap` 메뉴로 재현 가능). Build Settings 등록 확인 포함
- (S03) 개발용 UI는 **레거시 uGUI Text + 코드 생성** — TMP 기본 폰트는 한글 글리프가 없고(OS 폴백 없음) 씬에 수작업 UI를 넣으면 아트 교체 시 부담이라, 플레이스홀더 UI는 GardenScreen이 통째로 생성·폐기 가능하게 유지. 색 이모지는 환경에 따라 글리프가 안 보일 수 있으나 한글 라벨이 의미 전달 (정식 UI 트랙에서 TMP+한글 폰트로 교체)
- (S03) 프로젝트가 신형 Input System 전용(`activeInputHandler: 1`)이라 `WizardGarden.asmdef`에 `Unity.InputSystem` 참조 추가 — EventSystem은 `InputSystemUIInputModule` 사용 (`#if ENABLE_INPUT_SYSTEM` 분기)
