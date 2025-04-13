using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;


public class Ball : MonoBehaviour
{
    private const float MAX_MAGNITUTE = 200;
    private const float SLOW_TIMESCALE_MODE = 0.3f;
    private const float MINIMUM_MAGNITUTE_FOR_AIR_STATE = 0.1F;
    private const int BRICK_LAYER = 10;
    public const float MAX_POWER = 3;
    private const float MAX_SPEED = 5f;
    private const float LINE_LENGTH = 2.7f;
    private const float MAX_AIM_DISTANCE = 5f; // 최대 힘(MAX_POWER)에 도달하는 월드 조준 거리

    public Rigidbody rb = null;
    [SerializeField] private Plank plank = null;


    public enum BallState { FirstShoot, OnAir }
    public BallState ballState;

    public enum MaterialState { normal, flame };
    public MaterialState materialState;

    public bool mouseDown = false;


    private float power = 0;
    private Vector3 hitVector;
    private float dirMagnitute;

    private Camera cam;
    private Renderer ren;

    private Block currentBlock = null;

    [SerializeField] LineRenderer lineRenderer = null;
    [SerializeField] private LayerMask predictionLayerMask;

    private Vector3 lastFrameVelocity;

    private Vector3 screenPosition;



    private void Awake()
    {
        ballState = BallState.FirstShoot;
        cam = Camera.main;

    }

    private void Start()
    {
        rb.linearVelocity = Vector3.zero;
        ren = GetComponent<Renderer>();
        materialState = MaterialState.normal;
        currentBlock = GameObject.FindWithTag("Block")?.GetComponent<Block>();
        if (currentBlock == null)
        {
            Debug.LogError("Ball.Start: Failed to find GameObject with tag 'Block' and Block component.");
        }

    }



    private void Update()
    {
        if (ballState == BallState.FirstShoot)
        {
            if (currentBlock != null)
            {
                currentBlock.movable = false;
            }
            else
            {
                currentBlock = GameObject.FindWithTag("Block")?.GetComponent<Block>();
                if (currentBlock != null)
                {
                    currentBlock.movable = false;
                }
                else
                {
                    Debug.LogWarning("Ball.Update: currentBlock is null in FirstShoot state. Trying to find again failed.");
                }
            }
        }
        else if (ballState == BallState.OnAir)
        {
            if (currentBlock != null)
            {
                if (!currentBlock.movable)
                {
                    currentBlock.movable = true;
                }
            }
            else
            {
                currentBlock = GameObject.FindWithTag("Block")?.GetComponent<Block>();
                if (currentBlock != null)
                {
                    if (!currentBlock.movable)
                    {
                        currentBlock.movable = true;
                    }
                }
                else
                {
                    Debug.LogWarning("Ball.Update: currentBlock is null in OnAir state. Trying to find again failed.");
                }
            }
        }




        updateBallState();

        speedCheck();

        upperBoundCheck();

        if (ballState == Ball.BallState.FirstShoot && mouseDown)
        {
            calculateHitVector();
            drawLine();
        }

        // if (Skill.isFireballState)
        // {
        //     rb.velocity = rb.velocity.normalized * MAX_SPEED;
        // }

        // horizontalBoundsCheck(); // 화면 경계 체크 비활성화 (물리 벽 사용)
        lastFrameVelocity = rb.linearVelocity;
    }

    public void Launch(Vector3 direction, float force)
    {
        if (ballState == BallState.FirstShoot)
        {
            rb.AddForce(direction * force, ForceMode.Impulse);
            ballState = BallState.OnAir; // Immediately set state to OnAir
            // Ensure plank and block are movable after launch
            if (currentBlock != null)
            {
                 currentBlock.movable = true;
            }
            else
            {
                // Attempt to find block again if null during launch
                currentBlock = GameObject.FindWithTag("Block")?.GetComponent<Block>();
                 if (currentBlock != null)
                 {
                     currentBlock.movable = true;
                 }
                 else
                 {
                      Debug.LogError("Ball.Launch: currentBlock is null and couldn't be found.");
                 }
            }
        }
    }

    private void calculateHitVector()
    {
        Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
        Plane targetPlane = new Plane(Vector3.forward, transform.position);

        if (targetPlane.Raycast(mouseRay, out float distanceToPlane))
        {
            Vector3 targetPoint = mouseRay.GetPoint(distanceToPlane);
            Vector3 dir = targetPoint - transform.position;
            dir.z = 0; // 벽돌깨기 스타일로 Z축 이동은 무시

            // 힘(Power)을 월드 거리 기준으로 계산
            float worldDistance = dir.magnitude;
            // 월드 거리를 0~1 범위로 정규화하고 MAX_POWER를 곱함
            power = Mathf.Clamp01(worldDistance / MAX_AIM_DISTANCE) * MAX_POWER;

            // 방향 벡터(normalized)에 계산된 힘(power)을 곱하여 최종 hitVector 설정
            hitVector = dir.normalized * power;
        }
        else
        {
            Debug.LogWarning("Mouse ray did not intersect with the target plane.");
            hitVector = Vector3.zero; // 조준 실패 시 벡터 초기화
        }
    }

    private void drawLine()
    {
        float maxRayDistance = 15f;
        float reflectionLineLength = 2f;

        // hitVector가 계산되었는지 확인 (calculateHitVector에서 예외 처리 시 필요할 수 있음)
        if (hitVector == Vector3.zero)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        Vector3 launchDirection = hitVector.normalized;
        RaycastHit hit;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, transform.position);

        // --- 디버깅용 레이 그리기 (Scene 뷰에서 확인 가능) ---
        // 발사 방향 레이 (파란색)
        Debug.DrawRay(transform.position, launchDirection * maxRayDistance, Color.blue);
        // ----------------------------------------------------

        if (Physics.Raycast(transform.position, launchDirection, out hit, maxRayDistance, predictionLayerMask))
        {
            lineRenderer.SetPosition(1, hit.point);

            Vector3 reflectDir = Vector3.Reflect(launchDirection, hit.normal);

            lineRenderer.positionCount = 3;
            lineRenderer.SetPosition(2, hit.point + reflectDir * reflectionLineLength);

            // --- 디버깅용 레이 그리기 (Scene 뷰에서 확인 가능) ---
            // 반사 방향 레이 (노란색)
            Debug.DrawRay(hit.point, reflectDir * reflectionLineLength, Color.yellow);
            // ----------------------------------------------------
        }
        else
        {
            lineRenderer.SetPosition(1, transform.position + launchDirection * maxRayDistance);
        }
    }



    private void horizontalBoundsCheck()
    {
        screenPosition = Camera.main.WorldToScreenPoint(transform.position);

        if (screenPosition.x < 0)
        {


            float ratio = Mathf.Abs(rb.linearVelocity.y / rb.linearVelocity.x);



            if (ratio < 0.45)
            {
                float oldMagnitute = rb.linearVelocity.magnitude;
                rb.linearVelocity = new Vector3(0.5f, 0.5f, rb.linearVelocity.normalized.z) * oldMagnitute;
            }



            rb.linearVelocity = new Vector3(Mathf.Abs(rb.linearVelocity.x), rb.linearVelocity.y, rb.linearVelocity.z);

        }
        if (screenPosition.x > Screen.width)
        {
            float ratio = Mathf.Abs(rb.linearVelocity.y / rb.linearVelocity.x);

            if (ratio < 0.45)
            {
                float oldMagnitute = rb.linearVelocity.magnitude;
                rb.linearVelocity = new Vector3(-0.5f, 0.5f, rb.linearVelocity.normalized.z) * oldMagnitute;
            }
            rb.linearVelocity = new Vector3(-Mathf.Abs(rb.linearVelocity.x), rb.linearVelocity.y, rb.linearVelocity.z);
        }
    }

    private void speedCheck()
    {
        if (rb.linearVelocity.magnitude > MAX_SPEED)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * MAX_SPEED;
        }
    }

    private void upperBoundCheck()
    {
        if (currentBlock)
        {

            if (transform.position.y > currentBlock.topOfBlocksTransform.position.y)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -Mathf.Abs(rb.linearVelocity.y), rb.linearVelocity.z);
                materialState = MaterialState.normal;
                // Skill.isFireballState = false;
            }
        }
        else
        {
            currentBlock = GameObject.FindWithTag("Block").GetComponent<Block>();
        }
    }

    private void updateBallState()
    {
        if (rb.linearVelocity.magnitude > MINIMUM_MAGNITUTE_FOR_AIR_STATE)
        {
            ballState = BallState.OnAir;
        }
        else
        {
            ballState = BallState.FirstShoot;
        }
    }

    private void OnMouseDown()
    {
        if (ballState == BallState.FirstShoot)
        {
            mouseDown = true;
        }
    }

    private void OnMouseUp()
    {
        if (ballState == BallState.FirstShoot && mouseDown)
        {
            mouseDown = false;
            lineRenderer.positionCount = 0;
            rb.AddForce(hitVector, ForceMode.Impulse);
            if (currentBlock != null)
            {
                currentBlock.movable = true;
            }
        }
    }


    private void OnCollisionEnter(Collision other)
    {

        if (other.collider.tag == "Ground")
        {
            rb.useGravity = true;
            // plank.die();
            // if (Skill.isGoldenGroundState)
            // {
            //     reflect(other.contacts[0].normal);
            //     Skill.isGoldenGroundState = false;
            // }
            // else
            // {
            //     rb.useGravity = true;
            //     plank.die();
            // }

        }


        if (other.collider.tag == "BrickPart")
        {

            other.collider.gameObject.layer = BRICK_LAYER;
            other.collider.attachedRigidbody.isKinematic = false;
            reflect(other.contacts[0].normal);

            // if (Skill.isFireballState)
            // {
            //     rb.velocity = lastFrameVelocity;
            // }
            // else
            // {
            //     reflect(other.contacts[0].normal);
            // }
        }

        if (other.collider.tag == "Plank" && ballState != BallState.FirstShoot)
        {
            // rb.AddForce(other.impulse * PLANK_HIT_COEFICIENT, ForceMode.Impulse);
            reflect(other.contacts[0].normal);
        }

    }


    private void reflect(Vector3 normal)
    {
        var speed = lastFrameVelocity.magnitude;
        var direction = Vector3.Reflect(lastFrameVelocity.normalized, normal);
        rb.linearVelocity = direction * Mathf.Max(speed, 10);
    }
}
