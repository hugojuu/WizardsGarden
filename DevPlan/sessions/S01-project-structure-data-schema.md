# S01 — 프로젝트 구조 + 데이터 스키마 (M0)

**선행**: 없음 | **기획서 참조**: 3장(원소), 4·5·6장(데이터 형태만 훑기), 10장(견습생 스탯·희귀도)

## 목표
이후 모든 세션이 "코드 수정 없이 데이터 추가"로 콘텐츠를 넣을 수 있는 토대.

## In
- 폴더 구조: `Assets/Scripts` (asmdef), `Assets/Data` (SO 에셋), `Assets/Art/Placeholders`, `Assets/Tests`
- `ElementComposition` 구조체: **5칸 int 배열** (Fire/Water/Earth/Wind/Star⭐예약) + 연산 (합산, 일치 비교, 포함 여부, 최다 원소)
- enum: `Element`, `Season`, `Weather`(6종), `Rarity`(4단계), `Job`(3직군), `TimeOfDay`
- ScriptableObject 정의: `PlantData`(티어·조성·성장 시간·가치), `MaterialData`(가공 단계·조성·가치), `PotionData`(조성·지정 재료 목록·조건 태그·카테고리·판매가·장비 효과 ID), `ApprenticeData`(직군·4스탯·희귀도·패시브 ID 목록) — 필드는 기획서 표와 1:1
- 샘플 데이터: 티어1 식물 4종 + 포션 1종을 SO 에셋으로 생성

## Out
게임플레이 로직, UI, 세이브, 매칭 알고리즘(S05)

## 완료 기준
샘플 SO를 인스펙터에서 편집 가능, 컴파일 클린, 폴더/네이밍 컨벤션이 CLAUDE.md에 추가 기록됨
