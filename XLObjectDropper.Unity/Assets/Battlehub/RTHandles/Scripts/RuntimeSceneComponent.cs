﻿using Battlehub.RTCommon;
using Battlehub.Utils;
using System;
using UnityEngine;
namespace Battlehub.RTHandles
{
    public interface IRuntimeSceneComponent : IRuntimeSelectionComponent
    {
        RectTransform SceneGizmoTransform
        {
            get;
        }

        bool IsSceneGizmoEnabled
        {
            get;
            set;
        }

        SceneGizmo SceneGizmo
        {
            get;
        }

        [System.Obsolete("Use CanRotate instead")]
        bool CanOrbit
        {
            get;
            set;
        }

        bool CanRotate
        {
            get;
            set;
        }

        bool CanZoom
        {
            get;
            set;
        }

        float ZoomSpeed
        {
            get;
            set;
        }

        bool ConstantZoomSpeed
        {
            get;
            set;
        }

        bool ChangeOrthographicSizeOnly
        {
            get;
            set;
        }

        bool CanPan
        {
            get;
            set;
        }

        bool CanFreeMove
        {
            get;
            set;
        }

        GameObject GameObject
        {
            get;
        }
    }

    public class RuntimeSceneComponent : RuntimeSelectionComponent, IRuntimeSceneComponent
    {
        public Texture2D ViewTexture;
        public Texture2D MoveTexture;
        public Texture2D FreeMoveTexture;
        
        private Plane m_dragPlane;
        private Vector3 m_lastMousePosition;
        private bool m_lockInput;

        private IAnimationInfo m_focusAnimation;
        private Transform m_autoFocusTransform;
       
        [SerializeField]
        private SceneGizmo m_sceneGizmo;
        public SceneGizmo SceneGizmo
        {
            get { return m_sceneGizmo; }
        }

        [SerializeField]
        private RectTransform m_sceneGizmoTransform = null;
        [SerializeField]
        private bool m_isSceneGizmoEnabled = true;
        [SerializeField]
        private bool m_canPan = true;
        [SerializeField]
        private bool m_canZoom = true;
        [SerializeField]
        private bool m_changeOrthographicSizeOnly = true;
        [SerializeField]
        private bool m_canFocusTerrain = false;

        [SerializeField]
        private bool m_canRotate = true;
        [SerializeField]
        private bool m_canFreeMove = true;
        [SerializeField]
        private float m_orbitDistance = 5.0f;
        [SerializeField]
        private float m_zoomSpeed = 5.0f;
        [SerializeField]
        private bool m_constantZoomSpeed = false;
        [SerializeField]
        private float m_freeMovementSpeed = 10.0f;
        [SerializeField]
        private float m_freeRotationSpeed = 10.0f;

        private Quaternion m_targetRotation;
        private Vector3 m_targetPosition;
        private Quaternion m_prevCamRotation;
        private Vector3 m_prevCamPosition;
        private bool m_isSceneGizmoOrientationChanging;
        
        public bool IsSceneGizmoEnabled
        {
            get { return m_isSceneGizmoEnabled && m_sceneGizmo != null; }
            set
            {
                m_isSceneGizmoEnabled = value;
                if(m_sceneGizmo != null)
                {
                    m_sceneGizmo.gameObject.SetActive(value);
                }
            }
        }

        public RectTransform SceneGizmoTransform
        {
            get { return m_sceneGizmoTransform; }
        }

        public bool CanOrbit
        {
            get { return CanRotate; }
            set { CanRotate = value; }
        }

        public bool CanRotate
        {
            get { return m_canRotate; }
            set { m_canRotate = value; }
        }

        public bool CanZoom
        {
            get { return m_canZoom; }
            set { m_canZoom = value; }                 
        }

        public float ZoomSpeed
        {
            get { return m_zoomSpeed; }
            set { m_zoomSpeed = value; }
        }

        public bool ConstantZoomSpeed
        {
            get { return m_constantZoomSpeed; }
            set { m_constantZoomSpeed = value; }
        }

        public bool ChangeOrthographicSizeOnly
        {
            get { return m_changeOrthographicSizeOnly; }
            set { m_changeOrthographicSizeOnly = value; }
        }

        public bool CanPan
        {
            get { return m_canPan; }
            set { m_canPan = value; }
        }

        public bool CanFreeMove
        {
            get { return m_canFreeMove; }
            set { m_canFreeMove = value; }
        }

        public GameObject GameObject
        {
            get { return gameObject; }
        }

        public override Vector3 Pivot
        {
            get { return base.Pivot; }
            set
            {
                base.Pivot = value;
                m_orbitDistance = (Pivot - m_targetPosition).magnitude;
            }
        }

        public override Vector3 CameraPosition
        {
            get { return base.CameraPosition; }
            set
            {
                base.CameraPosition = value;
                Transform camTransform = Window.Camera.transform;
                m_targetPosition = camTransform.position;
                m_targetRotation = camTransform.rotation;
                m_orbitDistance = (Pivot - m_targetPosition).magnitude;
            }
        }

        public override bool IsOrthographic
        {
            get { return base.IsOrthographic; }
            set
            {
                if(m_sceneGizmo != null)
                {
                    if(m_sceneGizmo.IsOrthographic != value)
                    {
                        m_sceneGizmo.IsOrthographic = value;
                    }
                }
                else
                {
                    base.IsOrthographic = value;
                }
            }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            Window.IOCContainer.RegisterFallback<IRuntimeSceneComponent>(this);

            GameObject runGO = new GameObject("Run");
            runGO.transform.SetParent(transform, false);
            runGO.name = "Run";
            runGO.AddComponent<Run>();

            if (ViewTexture == null)
            {
                ViewTexture = Resources.Load<Texture2D>("RTH_Eye");
            }
            if (MoveTexture == null)
            {
                MoveTexture = Resources.Load<Texture2D>("RTH_Hand");
            }
            if(FreeMoveTexture == null)
            {
                FreeMoveTexture = Resources.Load<Texture2D>("RTH_FreeMove");
            }

            if (GetComponent<RuntimeSelectionInputBase>() == null)
            {
                gameObject.AddComponent<RuntimeSceneInput>();
            }

            if (m_sceneGizmo == null)
            {
                m_sceneGizmo = GetComponentInChildren<SceneGizmo>(true);
            }

            if (m_sceneGizmo != null)
            {
                if (m_sceneGizmo.Window == null)
                {
                    m_sceneGizmo.Window = Window;
                }
                m_sceneGizmo.OrientationChanging.AddListener(OnSceneGizmoOrientationChanging);
                m_sceneGizmo.OrientationChanged.AddListener(OnSceneGizmoOrientationChanged);
                m_sceneGizmo.ProjectionChanged.AddListener(OnSceneGizmoProjectionChanged);
                m_sceneGizmo.Pivot = PivotTransform;
                if (!IsSceneGizmoEnabled)
                {
                    m_sceneGizmo.gameObject.SetActive(false);
                }
            }

            Transform camTransform = Window.Camera.transform;
            camTransform.LookAt(Pivot);
            m_targetRotation = camTransform.rotation;
            m_targetPosition = camTransform.position;
            m_orbitDistance = (Pivot - m_targetPosition).magnitude;
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            Window.IOCContainer.UnregisterFallback<IRuntimeSceneComponent>(this);

            if (m_sceneGizmo != null)
            {
                m_sceneGizmo.OrientationChanging.RemoveListener(OnSceneGizmoOrientationChanging);
                m_sceneGizmo.OrientationChanged.RemoveListener(OnSceneGizmoOrientationChanged);
                m_sceneGizmo.ProjectionChanged.RemoveListener(OnSceneGizmoProjectionChanged);
            }
        }

        protected virtual void Update()
        {
            if (Editor.Tools.AutoFocus)
            {
                do
                {
                    if (Editor.Tools.ActiveTool != null)
                    {
                        break;
                    }

                    if (m_autoFocusTransform == null)
                    {
                        break;
                    }

                    if (m_autoFocusTransform.position == Pivot)
                    {
                        break;
                    }

                    if (m_focusAnimation != null && m_focusAnimation.InProgress)
                    {
                        break;
                    }

                    if(m_lockInput)
                    {
                        break;
                    }

                    Vector3 offset = (m_autoFocusTransform.position - SecondaryPivot);
                    Window.Camera.transform.position += offset;
                    PivotTransform.position += offset;
                    SecondaryPivotTransform.position += offset;
                }
                while (false);
            }

            if(Grid != null)
            {
                if (IsOrthographic)
                {
                    Transform camTransform = Window.Camera.transform;
                    UpdateGridRotation();
                    if (m_prevCamPosition != camTransform.position || m_prevCamRotation != camTransform.rotation)
                    {
                        m_prevCamPosition = camTransform.position;
                        m_prevCamRotation = camTransform.rotation;
                    }
                    else
                    {
                        if(!m_isSceneGizmoOrientationChanging)
                        {
                            Grid.Alpha += Time.deltaTime * 5;
                        }
                    }
                }
                else
                {
                    if(IsGridCloseToCamera())
                    {
                        Grid.Alpha -= Time.deltaTime * 25;
                    }
                    else
                    {
                        Grid.Alpha += Time.deltaTime * 5;
                    }
                }
            }
        }

        private bool IsGridCloseToCamera()
        {
            Transform camTransform = Window.Camera.transform;
            return Mathf.Abs(camTransform.position.y - Grid.transform.position.y) < 0.1f;
        }

        private void UpdateGridRotation()
        {
            Vector3 camForward = Window.Camera.transform.forward;
            if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(Vector3.right, camForward)), 1))
            {
                Grid.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
            else if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(Vector3.forward, camForward)), 1))
            {
                Grid.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
            else
            {
                Grid.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }

        public void UpdateCursorState(bool isPointerOverEditorArea, bool pan, bool rotate, bool freeMove)
        {
            if (!isPointerOverEditorArea)
            {
                Window.Editor.CursorHelper.ResetCursor(this);
                return;
            }

            if (freeMove && CanFreeMove)
            {
                Editor.CursorHelper.SetCursor(this, FreeMoveTexture, Vector2.one * 0.5f, CursorMode.Auto);
            }
            else if (pan && CanPan)
            {
                if (rotate && Editor.Tools.Current == RuntimeTool.View)
                {
                    Editor.CursorHelper.SetCursor(this, ViewTexture, Vector2.one * 0.5f, CursorMode.Auto);
                }
                else
                {
                    Editor.CursorHelper.SetCursor(this, MoveTexture, Vector2.one * 0.5f, CursorMode.Auto);
                }
            }
            else if (rotate && CanRotate)
            {
                Editor.CursorHelper.SetCursor(this, ViewTexture, Vector2.one * 0.5f, CursorMode.Auto);
            }
            else
            {
                Editor.CursorHelper.ResetCursor(this);
            }
        }

        public override void Focus()
        {
            if (m_lockInput)
            {
                return;
            }

            if (Selection.activeTransform == null)
            {
                return;
            }

            if (!m_canFocusTerrain && Selection.activeTransform.GetComponent<Terrain>())
            {
                return;
            }

            m_autoFocusTransform = Selection.activeTransform;
            if ((Selection.activeTransform.gameObject.hideFlags & HideFlags.DontSave) != 0)
            {
                return;
            }

            Bounds bounds = CalculateBounds(Selection.activeTransform);
            float objSize = Mathf.Max(bounds.extents.y, bounds.extents.x, bounds.extents.z) * 2.0f;
            float distance;
            if(ChangeOrthographicSizeOnly && IsOrthographic)
            {
                distance = m_orbitDistance;
            }
            else
            {
                float fov = Window.Camera.fieldOfView * Mathf.Deg2Rad;
                distance = Mathf.Abs(objSize / Mathf.Sin(fov / 2.0f));
            }

            PivotTransform.position = bounds.center;
            SecondaryPivotTransform.position = Selection.activeTransform.position;
            const float duration = 0.1f;

            m_focusAnimation = new Vector3AnimationInfo(Window.Camera.transform.position, PivotTransform.position - distance * Window.Camera.transform.forward, duration, Vector3AnimationInfo.EaseOutCubic,
                (target, value, t, completed) =>
                {
                    if (Window.Camera)
                    {
                        Window.Camera.transform.position = value;
                        m_targetPosition = value;
                    }
                });
            Run.Instance.Animation(m_focusAnimation);
            Run.Instance.Animation(new FloatAnimationInfo(m_orbitDistance, distance, duration, Vector3AnimationInfo.EaseOutCubic,
                (target, value, t, completed) =>
                {
                    m_orbitDistance = value;
                }));

            Run.Instance.Animation(new FloatAnimationInfo(Window.Camera.orthographicSize, objSize, duration, Vector3AnimationInfo.EaseOutCubic,
                (target, value, t, completed) =>
                {
                    if (Window.Camera)
                    {
                        Window.Camera.orthographicSize = value;
                    }
                }));
        }

        public virtual void Zoom(float deltaZ, Quaternion rotation)
        {
            if(m_lockInput)
            {
                return;
            }

            if (!CanZoom)
            {
                deltaZ = 0;
            }

            Camera camera = Window.Camera;

            if (camera.orthographic)
            {
                camera.orthographicSize -= deltaZ * camera.orthographicSize;
                if (camera.orthographicSize < 0.01f)
                {
                    camera.orthographicSize = 0.01f;
                }

                if (ChangeOrthographicSizeOnly)
                {
                    return;
                }
            }

            Vector3 fwd = (rotation * Vector3.forward) * deltaZ;
            if (m_constantZoomSpeed)
            {
                 fwd *= m_zoomSpeed;
            }
            else
            {
                fwd *= Mathf.Max(m_zoomSpeed, Mathf.Abs(m_orbitDistance));
            }
            
            Transform cameraTransform = Window.Camera.transform;
            m_orbitDistance = m_orbitDistance - fwd.z;

            fwd.z = 0;

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -m_orbitDistance);
            m_targetPosition = cameraTransform.TransformVector(fwd) + cameraTransform.rotation * negDistance + PivotTransform.position;
            cameraTransform.position = m_targetPosition;
        }


        public virtual void Orbit(float deltaX, float deltaY, float deltaZ)
        {
            if (m_lockInput || !CanRotate)
            {
                return;
            }
            if (deltaX == 0 && deltaY == 0 && deltaZ == 0)
            {
                return;
            }

            Transform cameraTransform = Window.Camera.transform;

            m_targetRotation = Quaternion.Inverse(
                Quaternion.Euler(deltaY, 0, 0) *
                Quaternion.Inverse(m_targetRotation) *
                Quaternion.Euler(0, -deltaX, 0));

            cameraTransform.rotation = m_targetRotation;

            Zoom(deltaZ, Quaternion.identity);
        }

        public void BeginPan(Vector3 mousePosition)
        {
            if (m_lockInput || !CanPan)
            {
                return;
            }
            m_lastMousePosition = mousePosition;
            
            RaycastHit hitInfo;
            if (Physics.Raycast(Window.Pointer, out hitInfo))
            {
                m_dragPlane = new Plane(-Window.Camera.transform.forward, hitInfo.point);
            }
            else
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                float d;
                Ray ray = Window.Pointer;
                if(groundPlane.Raycast(ray, out d))
                {
                    m_dragPlane = groundPlane;
                    m_dragPlane = new Plane(-Window.Camera.transform.forward, ray.GetPoint(d));
                }
                else
                {
                    m_dragPlane = new Plane(-Window.Camera.transform.forward, Window.Camera.transform.position + Window.Camera.transform.forward * 10);
                }

                //m_dragPlane = new Plane(-Window.Camera.transform.forward, PivotTransform.position);
                
            }
        }

        public void Pan(Vector3 mousePosition)
        {
            if (m_lockInput || !CanPan)
            {
                return;
            }
            Vector3 pointOnDragPlane;
            Vector3 prevPointOnDragPlane;

            if (GetPointOnDragPlane(mousePosition, out pointOnDragPlane) &&
                GetPointOnDragPlane(m_lastMousePosition, out prevPointOnDragPlane))
            {
                Transform camTransform = Window.Camera.transform;

                Vector3 delta = (pointOnDragPlane - prevPointOnDragPlane);
                m_lastMousePosition = mousePosition;
                camTransform.position -= delta;
                PivotTransform.position -= delta;
                SecondaryPivotTransform.position -= delta;

                m_targetPosition = camTransform.position;
            }
        }

        public void FreeMove(Vector2 rotate, Vector3 move, float forward)
        {
            if (m_lockInput || !CanFreeMove)
            {
                return;
            }

            Transform camTransform = Window.Camera.transform;

            m_targetRotation = Quaternion.Inverse(
                Quaternion.Euler(rotate.y, 0, 0) *
                Quaternion.Inverse(m_targetRotation) *
                Quaternion.Euler(0, -rotate.x, 0));

            camTransform.rotation = Quaternion.Slerp(
                camTransform.rotation,
                m_targetRotation,
                m_freeRotationSpeed * Time.deltaTime);

            Vector3 zoomOffset = Vector3.zero;
            if (Window.Camera.orthographic)
            {
                if (Mathf.Approximately(move.y, 0))
                {
                    move.y = forward;
                }
                else
                {
                    move.y /= 10.0f;
                }

                if (!Mathf.Approximately(move.y, 0))
                {
                    Vector3 position = camTransform.position;
                    Zoom(move.y, Quaternion.identity);
                    zoomOffset = camTransform.position - position;
                    camTransform.position = position;
                    move.y = 0;
                }
            }
            else
            {
                if (Mathf.Approximately(move.y, 0))
                {
                    move.y = forward * 50;
                }
            }

            m_targetPosition = m_targetPosition + zoomOffset +
                camTransform.forward * move.y + camTransform.right * move.x + camTransform.up * move.z;

            Vector3 newPosition = Vector3.Lerp(
                camTransform.position,
                m_targetPosition,
                m_freeMovementSpeed * Time.deltaTime);

            newPosition.x = (float)Math.Round(newPosition.x, 5);
            newPosition.y = (float)Math.Round(newPosition.y, 5);
            newPosition.z = (float)Math.Round(newPosition.z, 5);

            camTransform.position = newPosition;

            Vector3 newPivot = camTransform.position + camTransform.forward * m_orbitDistance;
            SecondaryPivotTransform.position += newPivot - Pivot;
            PivotTransform.position = newPivot;
        }

        private void OnSceneGizmoOrientationChanging()
        {
            m_lockInput = true;
            m_isSceneGizmoOrientationChanging = true;

            if (IsOrthographic)
            {
                Grid.Alpha = 0;
            }
        }

        private void OnSceneGizmoOrientationChanged()
        {
            m_lockInput = false;
            m_isSceneGizmoOrientationChanging = false;

            Pivot = Window.Camera.transform.position + Window.Camera.transform.forward * m_orbitDistance;
            SecondaryPivot = Pivot;

            m_targetRotation = Window.Camera.transform.rotation;
            m_targetPosition = Window.Camera.transform.position;
        }

        private void OnSceneGizmoProjectionChanged()
        {
            float fov = Window.Camera.fieldOfView * Mathf.Deg2Rad;
            float distance = (Window.Camera.transform.position - Pivot).magnitude;
            float objSize = distance * Mathf.Sin(fov / 2);
            Window.Camera.orthographicSize = objSize;

            if(!IsOrthographic)
            {
                Grid.transform.rotation = Quaternion.Euler(0, 0, 0);
                if (IsGridCloseToCamera())
                {
                    Grid.Alpha = 0;
                }
            }
        }

        private Bounds CalculateBounds(Transform t)
        {
            Renderer renderer = t.GetComponentInChildren<Renderer>();
            if (renderer)
            {
                Bounds bounds = renderer.bounds;
                if (bounds.size == Vector3.zero && bounds.center != renderer.transform.position)
                {
                    bounds = TransformBounds(renderer.transform.localToWorldMatrix, bounds);
                }
                CalculateBounds(t, ref bounds);
                if (bounds.extents == Vector3.zero)
                {
                    bounds.extents = new Vector3(0.5f, 0.5f, 0.5f);
                }
                return bounds;
            }

            return new Bounds(t.position, new Vector3(0.5f, 0.5f, 0.5f));
        }

        private void CalculateBounds(Transform t, ref Bounds totalBounds)
        {
            foreach (Transform child in t)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer)
                {
                    if(renderer is ParticleSystemRenderer)
                    {
                        continue; //Skip ParticleSystemRenderer rebderer
                    }

                    Bounds bounds = renderer.bounds;
                    if (bounds.size == Vector3.zero && bounds.center != renderer.transform.position)
                    {
                        bounds = TransformBounds(renderer.transform.localToWorldMatrix, bounds);
                    }
                    totalBounds.Encapsulate(bounds.min);
                    totalBounds.Encapsulate(bounds.max);
                }

                CalculateBounds(child, ref totalBounds);
            }
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            var center = matrix.MultiplyPoint(bounds.center);

            // transform the local extents' axes
            var extents = bounds.extents;
            var axisX = matrix.MultiplyVector(new Vector3(extents.x, 0, 0));
            var axisY = matrix.MultiplyVector(new Vector3(0, extents.y, 0));
            var axisZ = matrix.MultiplyVector(new Vector3(0, 0, extents.z));

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

        private bool GetPointOnDragPlane(Vector3 mouse, out Vector3 point)
        {
            Ray ray = Window.Camera.ScreenPointToRay(mouse);
            float distance;
            if (m_dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }
    }
}
