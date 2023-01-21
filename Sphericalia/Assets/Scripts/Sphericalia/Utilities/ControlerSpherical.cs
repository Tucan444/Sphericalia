using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlerSpherical : MonoBehaviour
{
    [SerializeField][Range(0.01f, 200)]public float speed = 40;
    [SerializeField][Range(0.01f, 360)]public float turnSpeed = 180;
    public bool sideWalking = false;
    public RigidbodySpherical rigidbody;

    Quaternion qq; // used to rotate direction in sidewalking

    SphSpaceManager ssm;
    SphericalUtilities su = new SphericalUtilities();

    Vector2 playerInput = new Vector2();

    public void GetDefaultSetup() {
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        GetDefaultSetup();
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    void OnValidate() {
        GetDefaultSetup();
    }

    void Start() {
    }

    // Update is called once per frame
    void Update()
    {
        // movement
        if (!sideWalking) {
            rigidbody.Move(playerInput[1] * speed * Time.deltaTime);
            rigidbody.Rotate(playerInput[0] * turnSpeed * Time.deltaTime);
        } else {
            float angleA = -su.Rad2Deg * (Mathf.Atan2(playerInput.y, playerInput.x) - su.HalfPI);
            qq = Quaternion.AngleAxis(angleA, rigidbody.position);

            rigidbody.direction = qq * rigidbody.direction;

            float norm = 0;
            if (playerInput.sqrMagnitude > 0) { norm = 1; }
            rigidbody.Move(norm * speed * Time.deltaTime, Quaternion.Inverse(qq));

            qq = Quaternion.identity;
        }

        // triggers
        rigidbody.Trigger();
    }

    public void OnMovement(InputAction.CallbackContext context) {
        Vector2 direction = context.ReadValue<Vector2>();
        playerInput = direction;
    }
}
