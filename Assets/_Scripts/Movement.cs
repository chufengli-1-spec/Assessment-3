using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    public float speed = 2f;
    public Animator animator;
    public AudioSource moveAudio;

    private Tweener tweener;
    private Vector3[] path;
    private int currentTarget = 0;
    private Vector3 nextTarget;
    private int currentDirection; 

    void Start()
    {
        path = new Vector3[]
        {
            new Vector3(-19, 9, 0),   
            new Vector3(-14, 9, 0),   
            new Vector3(-14, 6, 0),   
            new Vector3(-19, 6, 0)    
        };

        currentTarget = 0;
        nextTarget = path[1];

        transform.position = path[0];

        tweener = new Tweener(transform, path[0], nextTarget, speed);

        currentDirection = CalculateDirection(path[0], nextTarget);
        if (animator != null) animator.SetInteger("direction", currentDirection);

        if (moveAudio != null) moveAudio.Play();
    }

    void Update()
    {
        if (tweener != null)
        {
            bool finished = tweener.UpdateTween(Time.deltaTime);

            if (finished)
            {
                currentTarget = (currentTarget + 1) % path.Length;
                int nextIndex = (currentTarget + 1) % path.Length;
                nextTarget = path[nextIndex];

                currentDirection = CalculateDirection(path[currentTarget], nextTarget);
                if (animator != null) animator.SetInteger("direction", currentDirection);

                tweener = new Tweener(transform, path[currentTarget], nextTarget, speed);
            }
        }
    }

    int CalculateDirection(Vector3 start, Vector3 end)
    {
        Vector3 dir = (end - start).normalized;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return (dir.x > 0) ? 0 : 2; 
        else
            return (dir.y > 0) ? 3 : 1; 
    }

}
