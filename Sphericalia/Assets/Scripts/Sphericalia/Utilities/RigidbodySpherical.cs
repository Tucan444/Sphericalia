using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodySpherical : MonoBehaviour
{
    public Vector2 sphericalPosition = new Vector2();
    [SerializeField][Range(-Mathf.PI, Mathf.PI)]public float directionRotation = 0;
    public bool isCollider = true;
    public bool useColliderPool = false;
    public bool triggers = true;
    public bool useTriggerPool = false;
    public bool noSmoothing = true;
    [SerializeField][Range(0.1f, 3)]public float smoothSpeed = 1;
    public List<RigidbodyCircleCollider> colliders = new List<RigidbodyCircleCollider>();


    [HideInInspector] public Vector3 position = new Vector3(1, 0, 0);
    [HideInInspector] public Vector3 direction = new Vector3(0, 1, 0);
    Quaternion totalQ = Quaternion.identity;  // all movement from beggining to the end
    [HideInInspector] public Quaternion movementQ = Quaternion.identity;  // quaternion for movement from beggining to end with smoothing
    Quaternion moveQ; // last movement in total position to totalQ
    Quaternion Q = Quaternion.identity;  // used in default setup for rotating direction

    private bool started = false;

    SphSpaceManager ssm;
    SphericalUtilities su = new SphericalUtilities();

    [HideInInspector] public List<SphericalMovement> moves = new List<SphericalMovement>();
    [HideInInspector] public ColliderPool colliderPool = new ColliderPool();
    [HideInInspector] public TriggerPool triggerPool = new TriggerPool();

    [HideInInspector] public float additionalRotation = 0;

    public void GetDefaultSetup() {

        if (sphericalPosition[1] == 0) {
            sphericalPosition[1] += 0.0001f;
        }
        // setting cartesian position and initial direction
        position = su.Spherical2Cartesian(sphericalPosition);
        direction = su.GetDirection(position);
        if (sphericalPosition[1] < 0) {
            direction = -direction;
        }
        Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);
        direction = Q * direction;
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        GetDefaultSetup();
        transform.position = position;

        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    void OnValidate() {
        GetDefaultSetup();
        transform.position = position;
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(position, 0.05f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position  + (0.2f * direction));

        if (isCollider) {
            foreach (var collider in colliders) {
                Vector2 posAdded;
                if (!started) {
                    posAdded = su.AddSpherSpher(collider.sphPosition, sphericalPosition);
                    Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);
                    posAdded = su.Cartesian2Spherical(Q * su.Spherical2Cartesian(posAdded));
                } else {
                    posAdded = su.Cartesian2Spherical(collider.position);
                }

                Gizmos.color = Color.green * 1.4f;
                su.GizmosDrawPoints(su.GetCirclePoints(posAdded, collider.radius));
                Gizmos.color = Color.green * 1.2f;
                su.GizmosDrawPoints(su.GetCirclePoints(posAdded, collider.radius * 0.9f));
                Gizmos.color = Color.green;
                su.GizmosDrawPoints(su.GetCirclePoints(posAdded, collider.radius * 0.8f));
            }
        }
    }

    public void GizmosDrawLine(Vector3 a, Vector3 b, int n=4) {
        Vector3[] points = su.GetLinePoints(a, b, n);
        for (int i = 0; i < n-1; i++) {
            Gizmos.DrawLine(points[i], points[i+1]);
        }
    }

    public void SetUp(Vector3 position_, Vector3 direction_, float directionRotation_=5000) {
        sphericalPosition = su.Cartesian2Spherical(position_);
        position = position_;
        direction = direction_;
        directionRotation = directionRotation_ != 5000 ? directionRotation_ : directionRotation;
    }

    void Start() {
        for (int i = 0; i < colliders.Count; i++) {
            Vector2 posAdded = su.AddSpherSpher(colliders[i].sphPosition, sphericalPosition);
            Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);
            posAdded = su.Cartesian2Spherical(Q * su.Spherical2Cartesian(posAdded));

            colliders[i].sphPosition = posAdded;
            colliders[i].position = su.Spherical2Cartesian(posAdded);
        }
        started = true;
    }

    // Update is called once per frame 
    void Update()
    {
        // cleanup
        moves = new List<SphericalMovement>();
        colliderPool.Clear();
        triggerPool.Clear();
    }

    public Quaternion GetMoves() {
        Quaternion move = Quaternion.identity;
        for (int i = 0; i < moves.Count; i++)
        {
            move = moves[i].q * move;
        }
        if (!float.IsNaN(move.x)) {
        return move;} else {
            return Quaternion.identity;
        }
    }

    public Quaternion[] GetMovesVariants(Quaternion[] rotQ) {
        Quaternion[] variants = new Quaternion[4] {Quaternion.identity, Quaternion.identity, Quaternion.identity, Quaternion.identity};
        for (int i = 0; i < moves.Count; i++)
        {
            variants[0] = Quaternion.AngleAxis(moves[i].distance*0.5f, Vector3.Cross(position, rotQ[0] * moves[i].direction)) * variants[0];
            variants[1] = Quaternion.AngleAxis(moves[i].distance*0.75f, Vector3.Cross(position, rotQ[1] * moves[i].direction)) * variants[1];
            variants[2] = Quaternion.AngleAxis(-moves[i].distance*0.75f, Vector3.Cross(position, rotQ[1] * moves[i].direction)) * variants[2];
            variants[3] = Quaternion.AngleAxis(moves[i].distance*0.5f, Vector3.Cross(position, rotQ[0] * moves[i].direction)) * variants[3];
        }
        if (!float.IsNaN(variants[0].x) && !float.IsNaN(variants[1].x) && !float.IsNaN(variants[2].x) && !float.IsNaN(variants[3].x)) {
        return variants;} else {
            return new Quaternion[4] {Quaternion.identity, Quaternion.identity, Quaternion.identity, Quaternion.identity};
        }
    }

    public void Trigger() {
        if (!triggers) {
            return;
        }

        if (useTriggerPool) {
            foreach (var collider in colliders) {
                triggerPool.CollideTriggerCircle(collider.position, collider.radius);
            }
        } else {
            foreach (var collider in colliders) {
                ssm.CollideTriggerCircle(collider.position, collider.radius);
            }
        }
    }

    public void Move(float rAngle) {
        if (isCollider) {
            MoveWithCollision(rAngle);
        } else {
            MoveWithoutCollision(rAngle);
        }
    }

    public void Rotate(float rAngle) {
        if (isCollider) {
            RotateWithCollision(rAngle);
        } else {
            RotateWithoutCollision(rAngle);
        }

        additionalRotation = 0;
    }

    public void MoveWithoutCollision(float rAngle) {
        moveQ = Quaternion.AngleAxis(rAngle, Vector3.Cross(position, direction)) * GetMoves();
        if (float.IsNaN(moveQ.x)) {
            moveQ = GetMoves();
        }

        position = moveQ * position;
        direction = moveQ * direction;
        for (int i = 0; i < colliders.Count; i++) {
            colliders[i].position = moveQ * colliders[i].position;
        }

        totalQ = moveQ * totalQ;
        if (noSmoothing) {
            movementQ = totalQ;
        } else {
            movementQ = Quaternion.Lerp(movementQ, totalQ, Mathf.Min(4 * smoothSpeed * smoothSpeed * Time.deltaTime, 1));
        }

    }

    public void MoveWithCollision(float rAngle) {
        Quaternion[] rotQ = new Quaternion[2] {
            Quaternion.AngleAxis(90, position),
            Quaternion.AngleAxis(60, position)
        };

        Quaternion movesQ = GetMoves();
        Quaternion[] variants = GetMovesVariants(rotQ); 
        moveQ = Quaternion.AngleAxis(rAngle, Vector3.Cross(position, direction)) * movesQ;
        if (float.IsNaN(moveQ.x)) {
            moveQ = movesQ;
        }

        Quaternion moveQ1 = Quaternion.AngleAxis(rAngle*0.5f, Vector3.Cross(position, rotQ[0] * direction)) * variants[0];
        Quaternion moveQ2 = Quaternion.AngleAxis(rAngle*0.75f, Vector3.Cross(position, rotQ[1] * direction)) * variants[1];
        Quaternion moveQ3 = Quaternion.AngleAxis(-rAngle*0.75f, Vector3.Cross(position, rotQ[1] * direction)) * variants[2];
        Quaternion moveQ4 = Quaternion.AngleAxis(-rAngle*0.5f, Vector3.Cross(position, rotQ[0] * direction)) * variants[3];
        bool[] checks = new bool[5] {CheckMove(moveQ1), CheckMove(moveQ2), CheckMove(moveQ, triggers), CheckMove(moveQ3), CheckMove(moveQ4)};

        if (checks[2]) {
            if (checks[0] && checks[1] && checks[3] && checks[4]) {
                direction = direction;
                return;
            } else if (!checks[1]) {moveQ = moveQ2;
            } else if (!checks[3]) {moveQ = moveQ3;
            } else if (!checks[0]) {moveQ = moveQ1;
            } else {moveQ = moveQ4;}
        }
        
        position = moveQ * position;
        direction = moveQ * direction;
        for (int i = 0; i < colliders.Count; i++) {
            colliders[i].position = moveQ * colliders[i].position;
        }

        totalQ = moveQ * totalQ;
        if (noSmoothing) {
            movementQ = totalQ;
        } else {
            movementQ = Quaternion.Lerp(movementQ, totalQ, Mathf.Min(4 * smoothSpeed * smoothSpeed * Time.deltaTime, 1));
        }
    }   

    bool CheckMove(Quaternion q, bool triggerStuff=false) {  // if one collision found returns true
        Vector3 movedP;
        if (!useColliderPool) {
            foreach (var collider in colliders) {
                movedP = q * collider.position;
                if (ssm.CollideCircle(movedP, collider.radius, triggerStuff)) {
                    return true;
                }
            }
        } else {
            foreach (var collider in colliders) {
                movedP = q * collider.position;
                if (colliderPool.CollideCircle(movedP, collider.radius, triggerStuff)) {
                    return true;
                }
            }
        }

        return false;
    }

    public void RotateWithoutCollision(float rAngle) {
        Q = Quaternion.AngleAxis(rAngle + additionalRotation, position);
        if (!float.IsNaN(Q.x)) {
            direction = Q * direction;
            for (int i = 0; i < colliders.Count; i++) {
                colliders[i].position = Q * colliders[i].position;
            }

            totalQ = Q * totalQ;
            if (noSmoothing) {
                movementQ = totalQ;
            } else {
                movementQ = Quaternion.Lerp(movementQ, totalQ, Mathf.Min(4 * smoothSpeed * smoothSpeed * Time.deltaTime, 1));
            }
        }
    }

    public void RotateWithCollision(float rAngle) {
        Q = Quaternion.AngleAxis(rAngle + additionalRotation, position);
        Quaternion testQ = Quaternion.AngleAxis((rAngle + additionalRotation) * 0.5f, position);

        bool[] checks = new bool[2] {CheckMove(Q, triggers), CheckMove(testQ)};

        if (checks[0]) {
            if (checks[1]) {
                return;
            } else {
                Q = testQ;
            }
        }

        if (!float.IsNaN(Q.x)) {
            direction = Q * direction;
            for (int i = 0; i < colliders.Count; i++) {
                colliders[i].position = Q * colliders[i].position;
            }

            totalQ = Q * totalQ;
            if (noSmoothing) {
                movementQ = totalQ;
            } else {
                movementQ = Quaternion.Lerp(movementQ, totalQ, Mathf.Min(4 * smoothSpeed * smoothSpeed * Time.deltaTime, 1));
            }
        }
    }

    // alings direction in opposite of target
    public void AlignDirectionAgainstTarget(Vector3 target, float t, bool flip=false) {
        Vector3 pv = su.GetPlaneVector(position, su.SphLerp(position, target, 1));

        int sign = 1;
        if (Vector3.Dot(Vector3.Cross(position, direction), pv) > 0 ) {sign = -1;}
        float angleR = Mathf.PI - su.SphDistance(direction, pv);

        if (flip) {
            angleR = Mathf.PI - angleR;
            sign *= -1;
        }

        additionalRotation += su.Rad2Deg * t * angleR * sign;
    }
}

public class SphericalMovement {
    public Quaternion q;
    public Vector3 origin;
    public Vector3 direction;
    public float distance;
    public SphericalMovement(Vector3 origin_, Vector3 direction_, float distance_) {
        origin = origin_;
        direction = direction_;
        distance = distance_;
        q = Quaternion.AngleAxis(distance, Vector3.Cross(origin, direction));
    }
}

[System.Serializable]
public class RigidbodyCircleCollider {
    [Range(0, Mathf.PI)] public float radius = 0.1f;
    public Vector2 sphPosition = new Vector2(0, 0);
    [HideInInspector] public Vector3 position = new Vector3();  // used after game starts
}
