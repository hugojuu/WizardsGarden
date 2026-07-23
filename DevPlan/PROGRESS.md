# 진행 상태

**현재**: S05 완료 (2026-07-23, 조합 매칭 엔진 — 순수 C# 로직 + EditMode 184/184 통과) — 다음 세션은 **S06** (조합 UI·발견 연출·도감)

## 완료된 세션
- **S05 — 조합 매칭 엔진 (M2)** (2026-07-23)
  - 순수 C# 코어 신규 (`Assets/Scripts/Core`, ns `WizardGarden.Core`, **MonoBehaviour 비의존**):
    `BrewMatcher`(4단계 판정 엔진 — 레시피 목록을 `Dictionary<ElementComposition, List<BrewRecipe>>` 조성 인덱스로 변환해 주입받음. `Evaluate(inputs, ctx)` 집계형 + `Evaluate(총조성, id별개수, 총가치, ctx)` 직접형 2오버로드) · `BrewRecipe`/`IngredientRequirementSpec`/`BrewByproduct`/`FailureByproductSet`(순수 레시피·부산물 데이터) · `BrewResult`+`BrewOutcome`(Success/MissingIngredient/ConditionNotMet/FailureByproduct 4분류)+`FailureByproductKind`(Murky/Sediment/Mist) · `BrewInputItem`(투입 묶음: id·단위조성·단위가치·개수) · `IBrewContext`+`BrewContext`(조건 주입 인터페이스 — TimeOfDay/Weather/Season) · `BrewConditionInterpreter`(조건 태그 파서 — night_only / time: / weather: / season:)
  - 어댑터 (`Assets/Scripts`, ns `WizardGarden`): `BrewRecipeFactory`(PotionData(SO) → BrewRecipe 변환 + 부산물 세트 구성 — SO 접점을 여기 한 곳에 격리, S06이 이걸로 BrewMatcher 구성). 부산물 기본 id 상수 `potion_murky`/`potion_sediment`/`potion_mist`
  - 4단계 파이프라인: 1) 조성 매칭(합==레시피, star 슬롯 포함 5칸 동일성) → 2) 재료 지정(부족 시 `MissingIngredient`+힌트 "핵심 재료가 빠진 것 같다", 부족 수량 반환) → 3) 조건 게이트(불충족 시 `ConditionNotMet`+미충족 태그) → 4) 실패 부산물(총합≤4 또는 동률→탁한 / 최다 🔥·🌍→침전물 / 💧·💨→안개병, 판매가=min(표기가, 투입가치×30%))
  - **"같은 조성 다른 조건"**: 약초 포션(💧3🌍3 상시)/생명수 포션(💧3🌍3 weather:rain)이 조성 동일 → 조건이 더 구체적인(태그 수 많은) 레시피 우선 규칙. 비 오면 생명수, 아니면 약초 (기획서 6장 "숨김 규칙 1회만")
  - **코어·어댑터·씬 무변경**: 기존 S01~S04b 코드/세이브 v3 손대지 않음 (조합 엔진은 독립 신규 모듈)
  - 검증: 컴파일 에러/경고 0 · EditMode **184/184 통과**(기존 110 유지 + S05 신규 74: BrewMatcher 62 · ConditionInterpreter 12). 커버리지: 33종 전 레시피 성공(TestCaseSource 30 + 부산물 3분기) · 실패 3분기(총합/동률/최다원소) · 재료 지정 실패·부분 수량·힌트 · 조건부(야간/비/일식·재료+조건 결합) · "같은 조성 다른 조건" 우선순위 · **5번째 원소(별⭐) 슬롯 3케이스**(매칭 참여·조성 변화로 매칭 파괴·투입 집계 합산) · 판매가 30% 캡 · 투입 묶음 집계(조성합·id 카운트·가치합) · 경계(total 4/5 임계·빈 투입·조성 우선)
  - 완료 기준 대비: 4단계 판정 전부 그린 ✅ · 매칭 로직 MonoBehaviour 비의존(테스트에서 `new BrewMatcher(...)`) ✅

- **S04b — 맵 화면 전환 (Rusty형 프레젠테이션)** (2026-07-23) ★ M1 재검증
  - 신규 어댑터 (`Assets/Scripts`, ns `WizardGarden` — **코어 로직 무변경**, 전부 GameSession API 호출로만 동작):
    `MapScreen`(맵 오케스트레이터 — 월드 스페이스 구축·매 프레임 폴링 갱신·손님 tick·클릭 라우팅. 스모크 공용 API: `HandleWorldClick`/`GardenTileWorldPosition`/`BenchWorldPosition`/`ShopSlotWorldPosition`/`Popup`) · `MapTile`(클릭 대상 마커: GardenTile/Bench/ShopSlot + index, BoxCollider2D와 함께) · `MapPopup`(모달 목록 팝업 uGUI — 종자/가공/진열/확장 공용, 열 때마다 재구성, **닫은 뒤 액션 실행** — 해금 액션이 팝업을 다시 열어 목록 갱신) · `MapHud`(맵과 분리된 uGUI 레이어: 시계·골드·창고·힌트, 표시 전용 — 값은 MapScreen이 푸시) · `MapCustomerFx`(손님 연출: 판매 시 상점 앞 등장→2.6초 상승·페이드 — 유닛 AI는 S09) · `MapPlaceholderFactory`(흰 사각형 스프라이트 + 월드 TextMesh 생성 유틸)
  - 에디터: `MapSceneBootstrap` — 메뉴 **WizardGarden > Setup Map Scene (S04b)** (카메라 가로 구도·MapScreen 배치·SO 5+4종 연결·GameScreen 비활성, 재실행 안전) + **WizardGarden > Toggle Debug Screen (Play)**
  - **GameScreen 탭 UI → 디버그 화면 강등**: 씬에서 비활성 (삭제 안 함) — 플레이 중 **F12** 또는 위 메뉴로 토글. 비활성 상태라 Start 미실행 → 첫 토글 때 초기화됨
  - Player 설정 **`runInBackground = true`** (S04 인계 — 에디터/앱 무포커스 시 시간 정지 해소, 방치형 필수)
  - 세이브 변경 없음 (v3 그대로)
  - 검증: 컴파일 에러/경고 0 · EditMode **110/110 통과** (코어 무변경 증명) · 플레이 모드 스모크: **맵 클릭 경로만으로** 심기→수확→가공(마른 화염잎 30개)→진열(3칸×10)→판매 150G→밭 확장 20G(슬롯 5)→티어2 해금 100G→화염 양귀비 심기·수확까지 실제 실행. 디버그 화면 토글 확인. 스크린샷 `Captures/S04b_1~5_*.png` (git 미추적). 스모크 세이브는 삭제, 기존 세이브 원복
  - 완료 기준 대비: 풀 루프 맵 커서 조작 ✅ · **★10분 재미 판정은 유저 몫 — 미판정**
- **S04 — 공방 + 상점 → 수직 슬라이스** (2026-07-22) ★ M1
  - 순수 C# 코어 신규 (`Assets/Scripts/Core`): `Wallet`(골드 long — 경제 곡선 억 단위 대비, Add/TrySpend/CanAfford, Changed 이벤트) · `Workshop`(작업대 1개 — 시작 시 원료 소비, 출력 id·수량·시작 자원초만 저장, 진행도는 파생 계산 — GardenSlot과 동일 방침. processingSeconds는 SO 소관, 호출 시 전달) · `Shop`(진열대 3칸 + 손님 방문 타이머 — 자원초 기반 10초 주기, 손님 1명 = 첫 비어있지 않은 칸에서 한 종류 최대 5개 구매, 가격 조회는 델리게이트 주입. 빈 진열 방문도 주기 소모 → 밀린 손님 몰림 없음) · `UnlockState`(해금 id 집합, 중복 지불 방지) · `EconomyFormulas`(Cost(n) = 20 × 1.15^n 반올림 — 기획서 8장)
  - 코어 확장: `Inventory.TryRemove`(S03 인계 — 부족 시 실패, 0이 되면 항목 삭제) · `Garden.TryAddSlot` + `MaxSlotCount = 12`(플레이스홀더 UI 3×4 그리드 한계) · `GameSession`에 Wallet/Workshop/Shop/Unlocks 소유 + `NextGardenSlotCost`/`TryBuyGardenSlot`/`TryPurchaseUnlock`
  - 세이브 v3: `CurrentVersion` 2→3, 추가 필드 gold·unlockedIds·shopDisplaySlots·shopLastCustomerAtResourceSeconds·workshopOutputId/Count/StartedAt. 마이그레이션 case 2: 골드 0·빈 상태, **손님 타이머는 저장된 resourceSeconds로 초기화**(로드 직후 손님 몰림 방지)
  - 데이터 스키마: `ItemData.displayEmoji`로 이동(전 아이템 공통 — PlantData에서 승격, 직렬화 필드명 동일해 기존 에셋 값 보존) · `PlantData.unlockCost`(0 = 처음부터 해금) · `MaterialData`에 1차 가공 레시피 필드(sourceItem SO 참조·sourceCount·processingSeconds)
  - SO 에셋: 티어2 식물 1종 `Plant_FlamePoppy`(화염 양귀비 🔥2 — 15초·8G, 기획서 4장 / 해금 100G는 S04 결정) + 1차 가공 재료 4종 `Assets/Data/Materials`(마른 화염잎/이슬잎/흙풀/바람잎 — 조성 유지·가치 5G = 티어1 ×5, 가공 8초·원료 1개, 기획서 5장). `SampleDataBootstrap` 확장 — 메뉴 이름 **WizardGarden > Create Sample Data (S01+S04)** 로 변경
  - 어댑터 재구성: `GardenScreen` → **`GameScreen`** (정원/공방/상점 3탭 통합 — 상단 시계·골드 HUD, 탭 바, 공용 창고 패널, 정원 그리드는 슬롯 수 변경 시 재생성, 공방 작업대 박스+레시피 4버튼, 상점 진열대 3칸+손님 카운트다운+판매 로그 5줄, 종자/진열 선택 모달은 열 때마다 재구성. 손님 판매 tick은 탭과 무관하게 매 프레임 처리). 씬 구성 메뉴 **WizardGarden > Setup Game Scene (S04)** (`GameSceneBootstrap` — 구 GardenScreen 오브젝트 자동 제거, 재실행 안전). 구 `GardenScreen`/`GardenSceneBootstrap` 삭제
  - 검증: 컴파일 에러/경고 0 · EditMode 테스트 **110/110 통과**(기존 65 유지·2건 v3 대응 수정 + S04 신규 45: Wallet 7 · Workshop 8 · Shop 12 · Unlock 5 · Economy 3 · Inventory.TryRemove 4 · SaveMigrationV3/풀루프 6) · 플레이 모드 스모크로 **심기→수확→가공→진열→판매(125G)→밭 확장(20G, 다음 23G)→티어2 해금(100G)→티어2 심고 수확**까지 풀 루프 실제 실행 + 에디터 재시작 복원(골드·슬롯 5·해금·심긴 티어2 유지) 확인. 스크린샷 `Captures/S04_1~4_*.png` (git 미추적). 스모크 세이브는 삭제로 원상 복구 — 유저는 새 게임으로 판정 시작
  - 완료 기준 대비: 루프 완성 ✅ (구현·검증 완료) · **10분 재미 판정은 유저 몫 — 미판정** (판정 결과 이 파일에 기록 요망)
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
- (S05→S06) **매칭 엔진 사용법**: `var matcher = new WizardGarden.Core.BrewMatcher(recipes, byproducts);` → `matcher.Evaluate(inputs, context)`. `recipes`/`byproducts`는 `WizardGarden.BrewRecipeFactory.ToRecipes(potionDataList)` + `BrewRecipeFactory.BuildByproducts(murkySO, sedimentSO, mistSO)`로 SO에서 만든다(부산물 SO 없으면 기획서 기본값으로 대체). 입력은 `List<BrewInputItem>`(재료 id·단위조성·단위가치·개수). 결과 `BrewResult.Outcome`으로 분기: Success→`result.Recipe`(발견 연출·도감 등록·판매), MissingIngredient→`result.Hint`+`result.MissingIngredients`(id·부족수 — 브루트포스 방지 위해 UI는 재료명 노출 수위 조절), ConditionNotMet→`result.UnmetConditionTags`, FailureByproduct→`result.ByproductKind`/`result.Byproduct`/`result.ByproductSalePrice`
- (S05→S06) **재료 소비·산출은 엔진 밖**: 엔진은 순수 판정만 함(인벤토리 미조작). S06이 판정 후 `Inventory.TryRemove`로 투입 소비 + 산출 포션/부산물을 `Inventory.Add`. 포션 판매는 상점이 ItemData 공통으로 동작(S04 인계) → 포션 SO를 `GameScreen._itemsById`(또는 맵 상점 목록)에 등록 필요
- (S05→S06) **부산물 3종·포션 30종 SO 에셋 미생성**: 테스트는 코드 픽스처(`Assets/Tests/BrewFixture.cs`)로만 검증. S06(또는 데이터 세션)이 `Assets/Data/Potions`에 33종 SO를 만들 때 픽스처의 id/조성/판매가/조건태그를 그대로 옮기면 됨. 부산물 id는 `BrewRecipeFactory` 상수(potion_murky/sediment/mist)에 맞출 것
- (S05→S06) **지정 재료 id는 잠정**: 별빛 분말=`material_starlight_powder`, 용의 입김초=`plant_dragon_breath_herb`, 인어의 머리카락=`material_mermaid_hair`, 세계수 묘목=`plant_world_tree_sapling`, 무지개 수정 촉매=`material_rainbow_crystal`. 해당 식물·재료 테이블 authoring 시 실제 id와 일치시킬 것(불일치하면 재료 지정 레시피가 영영 실패)
- (S05→S11) **조건 주입 인터페이스**: `IBrewContext { TimeOfDay TimeOfDay; Weather Weather; Season Season; }`. S11이 실제 시간·날씨·계절 공급자를 이 인터페이스로 구현하면 됨(현재는 `GameClockRunner.Instance.Clock.CurrentTimeOfDay`로 TimeOfDay만 실값, 날씨·계절은 S11 전까지 Clear/Spring 고정 어댑터로). 조건 태그 문법: `night_only`·`time:{morning|day|evening|night}`·`weather:{clear|rain|storm|moonlit_night|meteor_shower|eclipse}`·`season:{spring|summer|autumn|winter}`. 인식 못한 태그는 불충족(false) 처리 — 신규 조건은 `BrewConditionInterpreter`에 파서 추가
- (S04b→S05) 조합 UI 진입점은 **작업대 클릭** (기획서 2-2장: 작업대/가마솥 클릭 = 조합 UI 창). 현재 `MapScreen.OnBenchClicked`가 idle이면 1차 가공 팝업을 여는데, S05는 여기서 조합 창으로 확장/분기할 것. `MapPopup`은 단순 목록형 — 자유 투입·발견형 조합 UI는 별도 창이 필요
- (S04b→S09) 견습생 유닛 세션 참조점: 맵 좌표계·구도 상수는 `MapScreen` 상단 const (정원 중심 -4.4,-0.4 / 작업대 3.1,1.7 / 진열대 행 4.6,-2.2 / 손님 지점 1.6,-3.35), 소팅오더 규약 = 지면 -100 · 구역 패치 -50 · 구역 라벨 -40 · 소품 -30 · 타일/시설 0~8 · 유닛(fx) 20~21. `MapCustomerFx`는 임시 연출 — S09 유닛 AI로 대체·확장
- (S04b) 배속 치트·오프라인 정산으로 손님 주기 여러 개가 **한 프레임에 몰리면** fx 라벨이 겹침 (x −0.85 간격 스폰). 실플레이(10초 간격)에선 1명씩이라 문제 없음 — S08 오프라인 정산 요약 팝업이 근본 해결
- (S04b) 디버그 화면(GameScreen)이 활성인 동안엔 MapScreen이 월드 클릭을 무시함. 둘 다 같은 `Shop.TickCustomers`를 호출하지만 주기 소모는 선착 한쪽만 (멱등 — 이중 판매 없음)
- (S04→S05/S06) 조합·포션 세션에서 쓸 접점: 재료 소비는 `Inventory.TryRemove`, 산출 골드는 `Wallet.Add`, 포션 진열·판매는 상점이 이미 **ItemData 공통**으로 동작(가격 = baseValue, 이모지 = displayEmoji)이라 `GameScreen._itemsById`에 포션 SO만 등록되면 그대로 팔림 — 현재 등록 소스는 `seedOptions`+`recipeOptions`뿐이므로 포션 목록 필드 추가 필요. 조건 게이트(night_only 등)는 `GameClockRunner.Instance.Clock.CurrentTimeOfDay` 참조
- (S04→S05) `Workshop`은 "출력 1종·원료 1종" 단순 모델 — 포션 조합(재료 여러 종·조건 게이트)은 Workshop 재사용이 아니라 별도 조합 시스템으로 만들 것 (공방 작업대는 가공 전용 유지)
- (S04→S08) 오프라인 정산 시 손님 방문도 정산 대상이 되면 `Shop.TickCustomers`가 그대로 처리함(경과 주기만큼 방문·재고 소진 시 자동 중단). 단 `AddResourceSeconds`로 자원초를 얹은 **다음 프레임**에 GameScreen.Update가 일괄 처리하는 구조라, 정산 연출(요약 팝업)을 원하면 Update 처리 전에 Core에서 직접 TickCustomers를 호출해 SaleRecord 목록을 회수할 것
- (S04) 에디터 무포커스 상태에선 플레이 모드 프레임이 멈춰 손님 판매가 진행 안 됨(`Application.runInBackground=false` 기본값) — 자동화 스모크 시 `runInBackground=true`를 켜고 시작할 것. 유저가 직접 플레이할 땐 문제 없음
- (S04) 데이터 리셋: 메뉴 이름이 **Create Sample Data (S01+S04)** 로 바뀜 (티어2 식물·재료 4종 포함 갱신)
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
- (S05) **결과 4분류를 하나의 `BrewResult` 구조체 + 팩토리로 통합** — Success/MissingIngredient/ConditionNotMet/FailureByproduct를 enum `Outcome`으로 분기하고 outcome별로 유효 필드만 채움. UI(S06)가 단일 반환값으로 스위치 처리
- (S05) **파이프라인 우선순위**: 같은 조성 후보군에서 (a) 재료+조건 전부 충족 후보가 있으면 성공(가장 구체적 조건 우선), (b) 없으면 재료는 됐는데 조건만 실패 → `ConditionNotMet`, (c) 재료부터 실패 → `MissingIngredient`. 즉 "조건 안내"가 "재료 안내"보다 우선 — 조성·재료가 맞았는데 시간만 틀린 경우가 더 진전된 상태라서. 33종엔 조성 충돌이 약초/생명수(둘 다 재료 없음)뿐이라 실무상 단일 후보 처리가 대부분
- (S05) **조건 불충족·재료 부족은 부산물을 만들지 않음** — 기획서 6장 "어느 것에도 불일치 시"에만 부산물(4단계). 조성이 어떤 레시피와 맞았다면(재료/조건만 부족) 부산물이 아니라 안내 결과. 실제로 재료·시간을 버리게 할지는 S06 UI 결정(엔진은 분류만)
- (S05) **현자의 엘릭서(🔥3💧3🌍3💨3 + ⭐별빛 분말) = 별빛 분말을 지정 재료로, 조성 star=0** 처리 — CLAUDE.md "별⭐ 예약, 현재 항상 0" 방침에 맞춰 출시판 조성에는 star를 넣지 않고 별빛 분말은 마음의 묘약의 "+무지개 수정 촉매"와 같은 촉매(지정 재료) 표기로 해석. 별 슬롯 동작은 별도 합성 테스트 3종으로 검증(파이프라인 배선 증명). **기획서가 star 수치를 명시 안 해 애매 — 아래 질문 참조**
- (S05) **무지개 수정 와일드카드(Out) vs 촉매 존재 판정(In) 구분** — 무지개 수정 촉매를 조성 치환 와일드카드로 쓰는 로직은 미구현(보너스 콘텐츠). 단 마음의 묘약은 촉매를 "지정 재료 존재 여부"로만 판정해 33종에 포함(재료+조건 결합 커버리지 확보)
- (S05) **실패 부산물 star 지배 케이스**: 최다 원소가 별⭐인 경우(예약 슬롯, 현재 미발생)는 기획서 미명세 → 안전하게 탁한 포션으로 기본 처리(주석·테스트로 명시)
- (S05) **판매가 계산 = `min(표기가, floor(투입가치 × 0.30))`, 음수 0 클램프** — 기획서 6장 악용 방지. 투입가치는 `BrewInputItem`의 단위가치×개수 합, 어댑터/테스트가 직접 넘기는 오버로드도 제공
- (S04b) **맵 좌표계·구도**: 월드 유닛 기준, 카메라 orthographic size 5(세로 10유닛) 고정 — 16:9에서 가로 ±8.9유닛 안에 전 구도 배치 (좌측 정원 3×4 그리드, 우상 공방, 우하 상점). 화면비가 더 좁으면 `MapScreen.SetupCamera`가 세로를 늘려 **가로 전폭을 보존** (미니 모드 대비 가로 구도 우선). 밭 12칸(상한)은 처음부터 전부 타일로 배치 — SlotCount 이후는 🔒 잠금 표시, 다음 구매 칸에만 가격 라벨, 잠긴 칸 클릭 = 확장 확인 팝업
- (S04b) **클릭 처리 = 중앙 레이캐스트**: 오브젝트별 OnMouseDown 대신 MapScreen이 Input System(`Mouse.current`)으로 `Physics2D.OverlapPoint` → `MapTile` 마커(종류+인덱스) → 라우팅. 팝업 열림·디버그 화면 활성·포인터가 uGUI 위면 월드 클릭 무시. 스모크 테스트도 같은 `HandleWorldClick` 경로 사용 (마우스 핸들러와 코드 공유)
- (S04b) **맵 텍스트 = TextMesh(레거시)** — 맵 오브젝트 uGUI 금지 방침. 이모지 글리프는 환경 따라 대체 문양으로 보일 수 있어 한글 라벨 병기 규약(S03) 유지. 성장 시각화 = 타일 안 식물 사각형 스케일(0.45/0.8/1.05) + 원소색 농도 + 이모지(🌱→식물) + 라벨(%→수확!)
- **(방향 전환, 2026-07-22) 프레젠테이션 = Rusty's Retirement형 맵+유닛으로 확정** — 기획서 2-2장 신설 (유저 결정: 커서 지시형·전체화면 기본·미니 모드 대비 가로 구도·조합만 UI 창). S03~S04의 코어 로직·테스트 110개는 전부 유지, `GameScreen` 탭 UI는 S04b에서 디버그 화면으로 강등. S04의 유저 재미 판정은 미실시 — **S04b 완료 후 맵 화면으로 재검증**. S09 브리프는 견습생 유닛 행동 포함으로 개정, S04b 브리프 신설
- (S04) **수치 결정** (기획서에 없는 값 — 재미 판정 후 조정 대상): 1차 가공 시간 **8초**(티어1 성장 3초의 ~2.7배 — 작업대 1개가 자연 병목이 되어 가마솥 추가·자동화 업그레이드 여지, 10분 안에 수십 사이클 가능) · 마른 잎 가치 **5G**(티어1 1G ×5 — 기획서 5장 ×4~5 상단) · 손님 주기 **10자원초**·1회 최대 **5개** 구매(가공품만 팔면 ~150G/10분 선 — 티어2 해금이 10분 판정 내에 옴) · 진열 1클릭 = 최대 **10개** 이동 · 밭 슬롯 기본가 **20G**(1.15^n: 20→23→26→30→35→40…) · 티어2 종자 해금 **100G**(기획서 8장 "티어 도약 = 수입 5~10분 분량"에 맞춤) · 밭 슬롯 상한 **12칸**(S04 UI 한계, 코어 상수라 상향 쉬움)
- (S04) **`displayEmoji`를 PlantData → `ItemData`(베이스)로 승격** — 재료·포션도 플레이스홀더 표시가 필요. 직렬화 필드명이 같아 기존 식물 에셋 값은 그대로 보존됨
- (S04) 가공 레시피는 별도 SO가 아니라 **`MaterialData` 자신이 레시피 보유**(sourceItem·sourceCount·processingSeconds) — 1차 가공은 산출물:레시피가 1:1이라 테이블 분리가 과설계. 2차·3차에서 다중 입력이 필요해지면 그때 재검토
- (S04) 상점 판매가는 **`baseValue` 그대로**(가격 조정·간판·VIP 없음 — 이후 경영 세션 몫). 손님 구매 규칙은 "앞 칸부터 한 종류만" — 진열 순서가 곧 판매 우선순위라는 최소한의 경영 선택지를 남김
- (S04) `GardenScreen` 확장 대신 **`GameScreen`으로 재구성** — 3구역 탭·공용 HUD·공용 창고가 화면 하나의 상태라서 분리 유지가 오히려 중복. S03의 폴링 방침·코드 생성 UI·플레이스홀더 규약은 그대로 승계
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
