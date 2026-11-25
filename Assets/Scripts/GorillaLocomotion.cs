// GorillaLocomotion.cs
// Port of the arm-swing VR movement from index.html's "GL" object.
// Original idea (and these default tuning numbers) come from the
// Gorilla Locomotion Unity package - this is a from-scratch
// reimplementation of that same approach, not a copy of its code.
//
// Requires: Unity XR Interaction Toolkit (or raw UnityEngine.XR input),
// a CharacterController on the player rig, and two hand Transforms
// driven by your VR hand tracking / controllers.
//
// Attach to the player rig root. Assign leftHand / rightHand to the
// Transforms that already follow your XR controllers.

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GorillaLocomotion : MonoBehaviour
{
    [Header("Hand references (assign the Transforms your XR rig already drives)")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform head; // the camera / HMD transform

    [Header("Tuning - same defaults as the web version's GL object")]
    public float maxArmLength = 1.5f;
    public float unstickDistance = 1f;
    public float velocityLimit = 0.4f;
    public float maxJumpSpeed = 6.5f;
    public float jumpMultiplier = 1.1f;
    public float handRadius = 0.05f;
    public float headRadius = 0.15f;
    public float slideFactor = 0.03f;
    [Range(0f, 1f)] public float precision = 0.995f;
    public int velocityFrameCount = 8;
    public float floorFriction = 10f; // "fric" in the web version
    public LayerMask collidableLayers = ~0;

    CharacterController cc;

    // Per-hand state: whether it's currently "stuck" to a surface, and
    // the world-space point it's stuck to.
    class HandState
    {
        public bool stuck;
        public Vector3 stickPoint;
        public Vector3 lastHandPos;
    }

    HandState left = new HandState();
    HandState right = new HandState();

    Vector3[] velocityFrames;
    int velocityFrameIndex;
    bool grounded;
    Vector3 verticalVelocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        velocityFrames = new Vector3[velocityFrameCount];
        left.lastHandPos = leftHand ? leftHand.position : Vector3.zero;
        right.lastHandPos = rightHand ? rightHand.position : Vector3.zero;
    }

    void FixedUpdate()
    {
        Vector3 frameMove = Vector3.zero;
        int contributingHands = 0;

        frameMove += StepHand(left, leftHand, ref contributingHands);
        frameMove += StepHand(right, rightHand, ref contributingHands);

        if (contributingHands > 0)
            frameMove /= contributingHands; // two hands on surfaces average out, like the web version

        // Record this frame's velocity for the launch-on-release average
        Vector3 instVel = frameMove / Time.fixedDeltaTime;
        velocityFrames[velocityFrameIndex] = instVel;
        velocityFrameIndex = (velocityFrameIndex + 1) % velocityFrames.Length;

        grounded = cc.isGrounded;

        if (grounded)
        {
            // Stop sliding once you let go, same as the "fric" fix in the web build
            verticalVelocity.x = Mathf.MoveTowards(verticalVelocity.x, 0f, floorFriction * Time.fixedDeltaTime);
            verticalVelocity.z = Mathf.MoveTowards(verticalVelocity.z, 0f, floorFriction * Time.fixedDeltaTime);
            if (verticalVelocity.y < 0) verticalVelocity.y = -0.5f;
        }
        else
        {
            verticalVelocity.y -= 9.8f * Time.fixedDeltaTime;
        }

        cc.Move(frameMove + verticalVelocity * Time.fixedDeltaTime);
    }

    Vector3 StepHand(HandState hs, Transform hand, ref int contributingHands)
    {
        if (!hand) return Vector3.zero;

        Vector3 handPos = hand.position;
        Vector3 delta = Vector3.zero;

        if (!hs.stuck)
        {
            // Try to stick: cast from the hand toward where it's moving / toward world geometry
            if (Physics.CheckSphere(handPos, handRadius, collidableLayers))
            {
                hs.stuck = true;
                hs.stickPoint = handPos;
            }
        }

        if (hs.stuck)
        {
            float distFromReal = Vector3.Distance(hs.stickPoint, handPos);

            // Unstick a hand that's now far from where the real hand is (you pulled away)
            if (distFromReal > unstickDistance)
            {
                hs.stuck = false;
            }
            else
            {
                // Moving the stuck point relative to the hand moves the body the opposite way,
                // i.e. "pulling" on a fixed point in the world moves you toward it.
                delta = hs.stickPoint - handPos;
                delta = Vector3.ClampMagnitude(delta, velocityLimit);
                contributingHands++;
            }
        }

        hs.lastHandPos = handPos;
        return delta;
    }

    /// <summary>
    /// Call this when a hand lets go of its last surface (e.g. grip released)
    /// to launch the player using the averaged recent velocity, same as the
    /// web version's "average speed of your last 8 frames" launch.
    /// </summary>
    public void ReleaseAndLaunch()
    {
        Vector3 avg = Vector3.zero;
        foreach (var v in velocityFrames) avg += v;
        avg /= velocityFrames.Length;

        Vector3 launch = avg * jumpMultiplier;
        if (launch.magnitude > maxJumpSpeed) launch = launch.normalized * maxJumpSpeed;

        verticalVelocity = launch;
        left.stuck = false;
        right.stuck = false;
    }
}
