using UnityEngine;

namespace WarForge.Controls
{
    /// <summary>
    /// Snapshot of player intent. Filled by TouchControls on device and by keyboard/mouse in the Editor.
    /// Gameplay reads only this struct, never raw touches, so AI and tests can drive the same code path.
    /// </summary>
    public struct DriveIntent
    {
        public float Throttle;     // -1..1
        public float Steer;        // -1..1
        public Vector2 AimDelta;   // turret look delta this frame (pixels or normalized)
        public bool Fire;
        public bool ToggleView;    // third person <-> gunner sight
        public bool CycleShell;
        public bool Pause;
        public bool Brake;
    }

    /// <summary>
    /// Landscape touch layout: left thumb = track throttle (up/down) and steering (left/right) via a virtual stick,
    /// right side = drag-to-aim, plus on-screen Fire, View and Shell buttons. Uses the legacy Input Manager
    /// (Project Settings > Player > Active Input Handling = Input Manager (Old) or Both).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls Instance { get; private set; }

        public float StickRadiusFraction = 0.12f;  // of screen height
        public bool ShowOnScreen = true;

        public DriveIntent Intent;

        int moveFinger = -1;
        int aimFinger = -1;
        Vector2 moveOrigin, aimLast;
        Vector2 moveVector;
        Texture2D white;
        GUIStyle label;
        readonly Rect[] buttons = new Rect[4];

        public const int BtnFire = 0, BtnView = 1, BtnShell = 2, BtnPause = 3;

        void Awake()
        {
            Instance = this;
            white = Texture2D.whiteTexture;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var i = Intent;
            i.Throttle = 0f;
            i.Steer = 0f;
            i.Brake = true;
            i.Fire = i.ToggleView = i.CycleShell = i.Pause = false;
            i.AimDelta = Vector2.zero;

            LayoutButtons();
            ReadTouches(ref i);
#if UNITY_EDITOR || UNITY_STANDALONE
            ReadKeyboardMouse(ref i);
#endif
            Intent = i;
        }

        void LayoutButtons()
        {
            float s = Screen.height;
            float bw = s * 0.2f, bh = s * 0.13f, pad = s * 0.03f;
            float x = Screen.width - bw - pad;
            buttons[BtnFire] = new Rect(x, s - bh - pad, bw, bh);
            buttons[BtnView] = new Rect(x - bw - pad, s - bh - pad, bw * 0.9f, bh);
            buttons[BtnShell] = new Rect(x, s - (bh + pad) * 2f - bh * 0.4f, bw * 0.9f, bh * 0.8f);
            buttons[BtnPause] = new Rect(pad, pad, bw * 0.6f, bh * 0.7f);
        }

        void ReadTouches(ref DriveIntent i)
        {
            if (UnityEngine.Input.touchCount == 0)
            {
                moveFinger = aimFinger = -1;
                moveVector = Vector2.zero;
                return;
            }

            float half = Screen.width * 0.5f;
            float stickR = Mathf.Max(1f, Screen.height * StickRadiusFraction);
            for (int k = 0; k < UnityEngine.Input.touchCount; k++)
            {
                Touch t = UnityEngine.Input.GetTouch(k);
                Vector2 pos = t.position;
                pos.y = Screen.height - pos.y;

                if (t.phase == TouchPhase.Began)
                {
                    int b = ButtonAt(pos);
                    if (b == BtnFire) i.Fire = true;
                    else if (b == BtnView) i.ToggleView = true;
                    else if (b == BtnShell) i.CycleShell = true;
                    else if (b == BtnPause) i.Pause = true;
                    else if (pos.x < half && moveFinger < 0)
                    {
                        moveFinger = t.fingerId;
                        moveOrigin = pos;
                    }
                    else if (pos.x >= half && aimFinger < 0)
                    {
                        aimFinger = t.fingerId;
                        aimLast = pos;
                    }
                }
                else if (t.fingerId == moveFinger)
                {
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        moveFinger = -1;
                        moveVector = Vector2.zero;
                    }
                    else
                    {
                        Vector2 d = (pos - moveOrigin) / stickR;
                        moveVector = Vector2.ClampMagnitude(d, 1f);
                    }
                }
                else if (t.fingerId == aimFinger)
                {
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) aimFinger = -1;
                    else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    {
                        i.AimDelta += (pos - aimLast) * 0.5f;
                        aimLast = pos;
                    }
                }
            }

            Vector2 mv = moveVector;
            i.Throttle = Mathf.Abs(mv.y) > 0.15f ? mv.y : 0f;
            i.Steer = Mathf.Abs(mv.x) > 0.15f ? mv.x : 0f;
            i.Brake = mv.magnitude < 0.05f;
        }

        int ButtonAt(Vector2 guiPos)
        {
            for (int b = 0; b < buttons.Length; b++) if (buttons[b].Contains(guiPos)) return b;
            return -1;
        }

        void ReadKeyboardMouse(ref DriveIntent i)
        {
            float kb = 0f, ks = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) kb += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) kb -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) ks += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) ks -= 1f;
            if (Mathf.Abs(kb) > 0f) i.Throttle = kb;
            if (Mathf.Abs(ks) > 0f) i.Steer = ks;
            if (UnityEngine.Input.GetMouseButton(1)) i.AimDelta += new Vector2(UnityEngine.Input.GetAxis("Mouse X"), -UnityEngine.Input.GetAxis("Mouse Y")) * 6f;
            if (UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetKeyDown(KeyCode.Space)) i.Fire = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.V) || UnityEngine.Input.GetMouseButtonDown(2)) i.ToggleView = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) i.CycleShell = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P)) i.Pause = true;
        }

        void OnGUI()
        {
            GUI.matrix = Matrix4x4.identity;
            if (!ShowOnScreen || Event.current.type != EventType.Repaint) return;
            if (label == null) label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(Screen.height * 0.035f) };
            label.normal.textColor = Color.white;

            DrawBtn(buttons[BtnFire], "FIRE", new Color(0.8f, 0.2f, 0.12f, 0.75f));
            DrawBtn(buttons[BtnView], "VIEW", new Color(0.2f, 0.25f, 0.2f, 0.7f));
            DrawBtn(buttons[BtnShell], "SHELL", new Color(0.25f, 0.25f, 0.3f, 0.7f));
            DrawBtn(buttons[BtnPause], "II", new Color(0f, 0f, 0f, 0.5f));

            if (moveFinger >= 0)
            {
                float r = Screen.height * StickRadiusFraction;
                Vector2 o = new Vector2(moveOrigin.x, moveOrigin.y);
                GUI.color = new Color(1, 1, 1, 0.25f);
                GUI.DrawTexture(new Rect(o.x - r, o.y - r, r * 2, r * 2), white);
                GUI.color = new Color(1, 1, 1, 0.6f);
                Vector2 knob = o + moveVector * r;
                GUI.DrawTexture(new Rect(knob.x - r * 0.35f, knob.y - r * 0.35f, r * 0.7f, r * 0.7f), white);
                GUI.color = Color.white;
            }
        }

        void DrawBtn(Rect r, string text, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = prev;
            GUI.Label(r, text, label);
        }
    }
}
