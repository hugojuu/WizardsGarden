# Wizard's Garden (마법사의 정원)

픽셀 아트 방치형 조합·경영 게임. Steam PC · Unity 2D (URP) · 1인 개발 · 가챠 없음 · **완성형 패키지 게임** (출시판 = 완결).

## 문서
- **기획서 (단일 진실 원천)**: `C:\Users\juwon\Downloads\wizards_garden_design.html` — 전체를 읽지 말고, 맡은 브리프가 지정한 장(章)만 발췌해 읽을 것
- **개발 플랜**: `DevPlan/README.md` (세션 분할 맵) · `DevPlan/PROGRESS.md` (진행 상태 — **세션 시작 시 필독, 종료 시 갱신**)
- 세션 브리프: `DevPlan/sessions/Sxx-*.md` — 한 세션 = 브리프 하나

## 핵심 아키텍처 결정 (변경하려면 기획서·이 파일 동시 갱신)
- **원소 조성은 처음부터 5칸 배열** (불🔥/물💧/대지🌍/바람💨/별⭐예약). 5번째 원소는 출시 후 보너스지만 데이터 구조는 지금부터 — 세이브 마이그레이션 방지
- **데이터 주도**: 식물 20·재료 15·포션 33·견습생 28은 전부 ScriptableObject 테이블. 콘텐츠 추가 = 코드가 아니라 데이터 작업
- **이중 시간**: 자원 시간(오프라인에도 진행, 8h 캡·효율 60%) vs 사건 시간(켜놨을 때만: 계절·날씨·VIP·모험·친밀도·스토리)
- **조합 판정 4단계**: 조성 매칭 → 재료 지정 → 조건 게이트 → 실패 부산물
- 게임 내 1일 = 현실 15분, 1계절 = 7일 = 현실 1시간
- 출시 로스터 22명 / 보너스 6명 (관계망 페어 단위 — 기획서 27장)

## 폴더 구조·네임스페이스·네이밍 (S01 확정)
- **폴더**: `Assets/Scripts`(코드, asmdef `WizardGarden`) · `Assets/Scripts/Core`(enum·구조체) · `Assets/Scripts/Data`(SO 정의) · `Assets/Scripts/Editor`(에디터 전용, asmdef `WizardGarden.Editor`) · `Assets/Data`(SO 에셋 — Plants/Potions/… 타입별 하위 폴더) · `Assets/Art/Placeholders` · `Assets/Tests`(EditMode, asmdef `WizardGarden.Tests`)
- **네임스페이스**: `WizardGarden.Core`(공용 타입) / `WizardGarden.Data`(SO) / `WizardGarden.EditorTools`(에디터) / `WizardGarden.Tests`
- **C# 네이밍**: 클래스·메서드·프로퍼티 PascalCase. **직렬화(인스펙터 노출) 필드는 camelCase, 비직렬화 private 필드는 _camelCase**. SO 데이터 클래스는 순수 데이터 컨테이너로 취급 — public camelCase 필드 + `[Tooltip]` 한국어 설명
- **SO 에셋 네이밍**: 파일명 `타입_영문PascalCase` (예: `Plant_EmberGrass`), `id` 필드는 `타입접두어_snake_case` (예: `plant_ember_grass`) — 세이브·크로스 참조는 항상 `id` 기준
- **데이터 스키마 핵심**: 식물·재료·포션은 공통 베이스 `ItemData`(id·displayName·composition·baseValue) 상속. `ElementComposition`은 [Serializable] struct — 5칸(불/물/대지/바람/별⭐예약) + 합산(+)·일치(Matches)·포함(Contains)·최다 원소(TryGetDominantElement, 동률 시 false) 연산. 포션 조건 태그는 `List<string>`(예: `night_only`, `weather:rain`) — 해석은 매칭 알고리즘(S05) 몫

## 작업 방식
- **기간 추정·일정 이야기는 하지 않는다.** 순서와 완료 기준만 (유저 확정 방침)
- 맡은 브리프의 **In 범위만** 구현. Out 범위는 다음 세션 몫이므로 미리 만들지 않는다
- 아트는 별도 트랙: 플레이스홀더(색 사각형 + 이모지/텍스트) 규약으로 개발, 후반에 교체
- 세션 종료 시 `DevPlan/PROGRESS.md`에 완료 항목·인계 메모·바뀐 결정을 기록
