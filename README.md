# Unity 실험실 쇼케이스 시스템

## 📂 프로젝트 구조
Assets/
├─ 3D Laboratory Environment with Appratus/
│ ├─ Documentation/
│ │ ├─ ClickableObject.cs
│ │ ├─ ShowcaseManager.cs
│ │ ├─ PlateDetector.cs
│ │ └─ PlateFollowCamera.cs
│ ├─ Prefabs/
│ │ └─ Rectangle Plate.prefab
│ └─ Scenes/
│ └─ LaboratoryScene.unity
├─ Packages/
├─ ProjectSettings/
└─ README.md

```markdown
# Unity 실험실 쇼케이스 시스템

## 📂 프로젝트 구조
```

Assets/
├─ 3D Laboratory Environment with Appratus/
│   ├─ Documentation/
│   │   ├─ ClickableObject.cs
│   │   ├─ ShowcaseManager.cs
│   │   ├─ PlateDetector.cs
│   │   └─ PlateFollowCamera.cs
│   ├─ Prefabs/
│   │   └─ Rectangle Plate.prefab
│   └─ Scenes/
│       └─ LaboratoryScene.unity
├─ Packages/
├─ ProjectSettings/
└─ README.md

```

## 🎯 기능 요약
1. **문 클릭** → 카메라 이동, 문 애니메이션, 뒤로가기 버튼 표시  
2. **OpenShowcase() 호출** → Plate 프리팹 생성  
3. **Plate 위에 오브젝트 올리면** → 메인테이블(Left/Right Anchor) 사이에 자동으로 Clone 생성  
4. **뒤로가기(CloseShowcase()) 호출** →  
   - Plate 위 원본은 원위치 복귀  
   - Plate 프리팹 파괴  
   - 메인테이블 위 Clone은 그대로 유지  

## ⚙️ 스크립트 및 Inspector 연결

### 1. ClickableObject.cs  
- **붙일 곳**: DoorAnimatorRoot  
- **필드 연결**:  
  - `cameraFocusPoint` → 문 내부 카메라 포커스 Transform  
  - `doorAnimator` → 문 Animator  
  - `openTrigger`/`closeTrigger` → 문 열기/닫기 트리거 이름  
  - `doorCollider` → 문 Collider  
  - `backButton` → 뒤로가기 버튼 GameObject  
  - `showcaseManager` → 씬의 ShowcaseManager 오브젝트  

### 2. ShowcaseManager.cs  
- **붙일 곳**: 씬의 빈 GameObject (`ShowcaseManager`)  
- **필드 연결**:  
  - `platePrefab` → Rectangle Plate 프리팹  
  - `plateSpawnPoint` → 빈 Transform (카메라 앞 생성 위치)  
  - `leftAnchor`/`rightAnchor` → 메인테이블 좌우 끝 빈 Transform  

### 3. PlateDetector.cs  
- **붙일 곳**: Plate Prefab 내 TriggerZone(자식)  
- **필드 연결**:  
  - `leftAnchor`/`rightAnchor` → 씬의 메인테이블 끝점  
  - `maxPerRow` → 한 줄에 최대 Clone 개수  
  - `excludeTag` → 복제 제외할 태그(예: “Door”)  

### 4. PlateFollowCamera.cs  
- **붙일 곳**: Plate Prefab 루트  
- **필드 연결**:  
  - `offsetFromCamera` → 카메라 대비 Plate 위치 오프셋  

## 📋 씬 세팅 체크리스트
- **Main Camera**  
- **PlateSpawnPoint** (카메라 자식, 위치·회전 설정)  
- **LeftAnchor**, **RightAnchor** (메인테이블 위 양 끝)  
- **ShowcaseManager** (빈 GameObject + ShowcaseManager 컴포넌트 + 필드 연결)  
- **DoorAnimatorRoot** (ClickableObject + Animator + ShowcaseManager 연결)  
- **MagneticZone** (Plate Prefab 자식, Trigger Collider + PlateDetector)  
- **Canvas/BackButton** (OnClick에 `ClickableObject.CloseAndReturn()` 및 `ShowcaseManager.CloseShowcase()` 연결)  
```
