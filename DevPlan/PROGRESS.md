# 진행 상태

**현재**: S01 완전 완료 (에디터 검증 포함) — 다음 세션은 **S02**

## 완료된 세션
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

## 개발 중 바뀐 결정
- (S01) `ElementComposition`은 "5칸 배열" 의미를 유지하되 **int[] 대신 고정 필드 5개 + `Element` 인덱서**로 구현 — struct 안의 배열은 참조 공유(값 의미론 파괴) 문제가 있어 값 타입 안전성과 인스펙터 편의를 위함. 슬롯 수 상수 `ElementComposition.SlotCount = 5`
- (S01) 식물·재료·포션은 공통 베이스 `ItemData` 상속 (기획서 3장 "모든 식물·재료·포션은 원소 조성을 가짐"의 코드 반영). 포션 지정 재료는 `ItemData` 참조로 식물·가공재료 모두 지정 가능
- (S01) 포션 조건(야간/날씨 등)은 enum이 아닌 **문자열 태그 목록**(`conditionTags`)으로 저장 — 조건 종류 추가를 데이터 작업으로 유지, 해석은 S05 매칭 알고리즘에서
