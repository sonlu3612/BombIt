// using UnityEngine;

// public class PlayerMovement : MonoBehaviour
// {
//     private Rigidbody2D rb;

//     void Awake()
//     {
//         rb = GetComponent<Rigidbody2D>();
//     }

//     public void Move(Vector2 dir, float speed)
//     {
//         if (dir == Vector2.zero)
//         {
//             rb.linearVelocity = Vector2.zero;
//             return;
//         }

//         dir = dir.normalized;
//         // rb.linearVelocity = dir * speed;
//         Vector2 targetVel = dir * speed;

//         rb.linearVelocity = new Vector2(targetVel.x, targetVel.y);
//     }
// }

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private bool isMoving = false;
    private Vector3 targetPos;

    public LayerMask obstacleLayer;

    void Update()
    {
        if (isMoving) return;

        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // chặn đi chéo
        if (input.x != 0) input.y = 0;

        if (input != Vector2.zero)
        {
            Vector3 dir = new Vector3(input.x, input.y, 0);
            Vector3 nextPos = transform.position + dir;

            // check có vật cản không
            if (!Physics2D.OverlapCircle(nextPos, 0.3f, obstacleLayer))
            {
                StartCoroutine(MoveTo(nextPos));
            }
        }
    }

    System.Collections.IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;

        while ((target - transform.position).sqrMagnitude > 0.001f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = target;
        isMoving = false;
    }
}