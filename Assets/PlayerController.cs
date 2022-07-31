using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    const int TRANS_TIME = 3;
    const int ROT_TIME = 3;

    const int FALL_COUNT_UNIT = 60;
    const int FALL_COUNT_SPD = 5;
    const int FALL_COUNT_FAST_SPD = 10;
    const int GROUND_FRAMES = 50;

    int _fallCount = 0;
    int _groundFrame = GROUND_FRAMES;

    enum RotState
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,

        Inavalid = -1,
    }

    [SerializeField] PuyoCntroller[] _puyoControllers = new PuyoCntroller[2] { default!, default! };
    [SerializeField] BoardController boardController = default!;

    AnimationController _animationController = new AnimationController();
    Vector2Int _last_position;
    RotState _last_rotate;

    Vector2Int _position;
    RotState _rotate = RotState.Up;

    LogicalInput logicalInput = new();

    // Start is called before the first frame updat
    void Start()
    {
        _puyoControllers[0].SetPuyoType(PuyoType.Green);
        _puyoControllers[1].SetPuyoType(PuyoType.Red);

        _position = new Vector2Int(2, 12);
        _rotate = RotState.Up;

        _puyoControllers[0].SetPos(new Vector3((float)_position.x, (float)_position.y, 0.0f));
        Vector2Int posChild = CalcChildPuyoPos(_position, _rotate);
        _puyoControllers[1].SetPos(new Vector3((float)posChild.x, (float)posChild.y + 1.0f, 0.0f));
    }

    static readonly Vector2Int[] rotate_tbl = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    private static Vector2Int CalcChildPuyoPos(Vector2Int pos, RotState rot)
    {
        return pos + rotate_tbl[(int)rot];
    }

    private bool CanMove(Vector2Int pos, RotState rot)
    {
        if (!boardController.CanSettle(pos)) return false;
        if (!boardController.CanSettle(CalcChildPuyoPos(pos, rot))) return false;

        return true;
    }

    void SetTransition(Vector2Int pos,RotState rot,int time)
    {
        _last_position=_position;
        _last_rotate = _rotate;

        _position = pos;
        _rotate = rot;

        _animationController.Set(time);
    }

    private bool Translate(bool is_right)
    {
        Vector2Int pos = _position + (is_right ? Vector2Int.right : Vector2Int.left);
        if (!CanMove(pos, _rotate)) return false;

        //_position = pos;

        SetTransition(pos, _rotate, TRANS_TIME);

        //�K�v�H
     /* _puyoControllers[0].SetPos(new Vector3((float)_position.x, (float)_position.y, 0.0f));
        Vector2Int posChild = CalcChildPuyoPos(_position, _rotate);
        _puyoControllers[1].SetPos(new Vector3((float)posChild.x, (float)posChild.y, 0.0f)); */

        return true;
    }

    bool Rotate(bool is_raight)
    {
        RotState rot = (RotState)(((int)_rotate + (is_raight ? +1 : +3)) & 3);

        Vector2Int pos = _position;
        
        switch (rot)
        {
            case RotState.Down:
                if (!boardController.CanSettle(pos + Vector2Int.down) || !boardController.CanSettle(pos + new Vector2Int(is_raight ? 1 : -1, -1))) 
                {
                    pos += Vector2Int.up;
                }
                break;

            case RotState.Right:
                if (!boardController.CanSettle(pos + Vector2Int.right)) pos += Vector2Int.left;
                break;

            case RotState.Left:
                if (!boardController.CanSettle(pos + Vector2Int.left)) pos += Vector2Int.right;
                break;

            case RotState.Up:
                break;

            default:
                Debug.Assert(false);
                break;
        }

        if (!CanMove(pos, rot)) return false;

        SetTransition(pos, rot, ROT_TIME);

        //�K�v�H
     /* _position = pos;
        _rotate = rot;

        _puyoControllers[0].SetPos(new Vector3((float)_position.x, (float)_position.y, 0.0f));
        Vector2Int posChild = CalcChildPuyoPos(_position, _rotate);
        _puyoControllers[1].SetPos(new Vector3((float)posChild.x, (float)posChild.y, 0.0f)); */

         return true;
    }

    void Settle()
    {
        bool is_set0 = boardController.Settle(_position, (int)_puyoControllers[0].GetPuyoType());
        Debug.Assert(is_set0);

        bool is_set1 = boardController.Settle(CalcChildPuyoPos(_position, _rotate), (int)_puyoControllers[1].GetPuyoType());
        Debug.Assert(is_set1);

        gameObject.SetActive(false);
    }

    void QuickDrop()
    {
        Vector2Int pos = _position;

        do
        {
            pos += Vector2Int.down;
        } while (CanMove(pos, _rotate));
        pos -= Vector2Int.down;

        _position = pos;

        Settle();

        /*bool is_set0 = boardController.Settle(_position, (int)_puyoControllers[0].GetPuyoType());
        Debug.Assert(is_set0); */

        bool is_set1 = boardController.Settle(CalcChildPuyoPos(_position, _rotate), (int)_puyoControllers[1].GetPuyoType());
        Debug.Assert(is_set1);

        gameObject.SetActive(false);
    }

    static readonly KeyCode[] key_code_tbl = new KeyCode[(int)LogicalInput.Key.Max]{
        KeyCode.RightArrow,
        KeyCode.LeftArrow,
        KeyCode.X,
        KeyCode.Z,
        KeyCode.UpArrow,
        KeyCode.DownArrow
    };

    void UpdateInput()
    {
        LogicalInput.Key inputDev = 0;

        for(int i = 0; i < (int)LogicalInput.Key.Max; i++)
        {
            if (Input.GetKey(key_code_tbl[i]))
            {
                inputDev |= (LogicalInput.Key)(1 << i);
            }
        }

        logicalInput.Update(inputDev);
    }

    bool Fall(bool is_fast)
    {
        _fallCount -= is_fast ? FALL_COUNT_FAST_SPD : FALL_COUNT_SPD;

        while (_fallCount < 0)
        {
            if (!CanMove(_position + Vector2Int.down, _rotate))
            {
                _fallCount = 0;
                if (0 < --_groundFrame) return true;

                Settle();
                return false;
            }

            _position += Vector2Int.down;
            _last_position += Vector2Int.down;
            _fallCount += FALL_COUNT_UNIT;
        }

        return true;
    }

    void Control()
    {
        if (!Fall(logicalInput.IsRaw(LogicalInput.Key.Down))) return;

        if (_animationController.Update()) return;

        //���s�ړ�
        if (logicalInput.IsRepeat(LogicalInput.Key.Right))
        {
            if(Translate(true)) return;
        }
        if (logicalInput.IsRepeat(LogicalInput.Key.Left))
        {
            if(Translate(false)) return;
        }

        //��]
        if (logicalInput.IsTrigger(LogicalInput.Key.RotR))
        {
            if(Rotate(true)) return;
        }
        if (logicalInput.IsTrigger(LogicalInput.Key.RotL))
        {
            if(Rotate(false)) return;
        }

        //�N�C�b�N�h���b�v
        if (logicalInput.IsRelease(LogicalInput.Key.QuickDrop))
        {
            QuickDrop();
        }
    }

    void FixedUpdate()
    {
        UpdateInput();

        Control();
        

        Vector3 dy = Vector3.up * (float)_fallCount / (float)FALL_COUNT_UNIT;
        float anim_rate = _animationController.GetNormalized();
        _puyoControllers[0].SetPos(Interpolate(_position, RotState.Inavalid, _last_position, RotState.Inavalid, anim_rate));
        _puyoControllers[1].SetPos(Interpolate(_position, _rotate, _last_position, _last_rotate, anim_rate));
    }

    // Update is called once per frame
    void Update()
    {
        if (!_animationController.Update(/*Time.deltaTime*/))
        {
            Control();
        }

        float anim_rate = _animationController.GetNormalized();
        _puyoControllers[0].SetPos(Interpolate(_position, RotState.Inavalid, _last_position, RotState.Inavalid, anim_rate));
        _puyoControllers[1].SetPos(Interpolate(_position, _rotate, _last_position, _last_rotate, anim_rate));
    }

    static Vector3 Interpolate(Vector2Int pos, RotState rot, Vector2Int pos_last, RotState rot_last, float rate)
    {
        Vector3 p = Vector3.Lerp(
            new Vector3((float)pos.x, (float)pos.y, 0.0f),
            new Vector3((float)pos_last.x, (float)pos_last.y, 0.0f), rate);

        if (rot == RotState.Inavalid) return p;

        float theta0 = 0.5f * Mathf.PI * (float)(int)rot;
        float theta1 = 0.5f * Mathf.PI * (float)(int)rot_last;
        float theta = theta1 - theta0;

        if (+Mathf.PI < theta) theta = theta - 2.0f * Mathf.PI;
        if (theta< -Mathf.PI) theta = theta + 2.0f * Mathf.PI;

        theta = theta0 + rate * theta;

        return p + new Vector3(Mathf.Sin(theta), Mathf.Cos(theta), 0.0f);
    }
}

