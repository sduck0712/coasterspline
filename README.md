S-Grip Roller Coaster – Science Lab (Unity)

핵심: 노트북 기반 핸드트래킹 과학 실험용 롤러코스터 콘텐트.
모드: Explore(탐색) → Experiment(실험) → Challenge(챌린지)

1. 환경 & 의존성

Unity 2021/2022/2023 LTS (URP 사용 가능)

TextMeshPro 포함

(선택) Terrain

2. 폴더 구조 (요약)
Assets/
  CoasterSpline/
    Scripts/
      myScripts/
        AppController.cs
        GameModeManager.cs
        BoomerangController.cs
        StartRegionBinder.cs
        HUD_All_TMP.cs
        FrictionController.cs
        // (옵션)
        CoasterSensorRuntime.cs
        AcceleratorForceApplier.cs
    ...
  Scenes/
    Demo.unity
  Docs/
    README.md
    images/
      overview.png
      wiring_binder.png
      wiring_boomerang.png
      ui_explore.png

3. 씬 와이어링
3.1 GameMode & App

GameObject: GameModeManager

Buttons → OnClick:

Explore → SetModeExplore()

Experiment → SetModeExperiment()

Challenge → SetModeChallenge()

Start → StartRun()

End → EndRun()

Reset → ResetRun()

App 슬롯 → AppController 드래그

GameObject: AppController

startBinder → StartRegionBinder

trainRb → Train(선두 카트) Rigidbody

friction → Train의 FrictionController

coasterCam → 팔로우 카메라 스크립트

mouseOrbit → 마우스 드래그 카메라 스크립트

heightSlider, heightInput, massInput, startSpeedInput, frictionToggle 연결

3.2 StartRegionBinder (탐색 높이)

stationRoot (스테이션 기준 Transform)

trainRoot (Train 루트)

Absolute Height 사용:

useAbsoluteHeight = ✅

terrain 지정

minClearance / maxAboveGround 설정

범위

affectAllChains = ❌

autoDetectAnchors = ✅ or 수동 chainIndex/startAnchor/anchorCount

alwaysIncludeBehindTrain = ✅ (기차 뒤쪽(X–)은 항상 포함)

3.3 BoomerangController (플로우)

station.accelerators / lifthill1.accelerators / lifthill2.accelerators
→ CoasterAccelerator들을 배열로 채우기

stationSensor / lifthill1TopSensor / lifthill2TopSensor 할당

(옵션) autoStartOnPlay = ❌ (교육용)

3.4 HUD / Physics

HUD_All_TMP: Train 높이/속도/에너지 표시

FrictionController: 마찰 on/off (토글로 제어)

(옵션) CoasterSensorRuntime + AcceleratorForceApplier → 간이 파이프라인

4. 사용법 (교육 흐름)
Explore (탐색)

높이 슬라이더/숫자로 스테이션 & 뒤쪽 레일 높이 조절

질량/마찰/초기속도 조정

Free Cam 토글로 자유 시야

Start → 주행 시작

Experiment (실험)

데이터 로깅(속도/높이/에너지) + 그래프/CSV (추가 예정)

Challenge (챌린지)

목표 달성형 미션(마찰 ON/OFF, 제한 높이 등) (추가 예정)

5. 자주 겪는 이슈 & 해결

Start 눌러도 안 감

GameModeManager → 콘솔에 [Run Start] 로그 확인

BoomerangController가 씬에 있고 OnRunStart 리스너가 등록되어야 함

BoomerangController의 accelerators 배열이 비어 있지 않은지

Train Rigidbody: isKinematic = false, mass/drag 정상

테스트용 startSpeed 1~2 m/s 입력

Accelerator 색이 안 변함

Force가 0이면 노랑에 가깝게 보임(샘플 색상 로직 주의)

Force가 설정되었는지, ApplyGroup이 호출되었는지 확인

센서 이벤트가 안 터짐

원본 감지 스크립트가 없다면 CoasterSensorRuntime를 Train에 붙여 거리기반으로 호출

레일 전부가 같이 움직임

StartRegionBinder.affectAllChains = ❌

스테이션 기둥이 바닥까지 안 닿음

StartRegionBinder의 stretchSupportsToGround(해당 기능이 있는 버전) 또는
지원물(서포트) 길이 조정 로직 사용

6. 팀 코딩(코덱스/AI IDE) 팁

모든 주요 스크립트에 요약 주석(XML/요약)과 공용 필드의 툴팁을 유지해 주세요.

README의 와이어링 이미지를 최신 상태로 갱신하면, 코드 어시스트가 “의도”를 잘 추적합니다.

이슈가 생기면 스크린샷 + 콘솔 로그 + 인스펙터 캡처를 함께 올리면 분석 속도가 3배 빨라집니다.
