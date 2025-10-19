using UnityEngine;

public class Tweener
{
    public Transform target;
    private Vector3 startPos;
    private Vector3 endPos;
    private float duration;
    private float elapsedTime;

    public Tweener(Transform target, Vector3 start, Vector3 end, float speed)
    {
        this.target = target;
        this.startPos = start;
        this.endPos = end;

        float distance = Vector3.Distance(start, end);
        this.duration = distance / speed;  
        this.elapsedTime = 0f;

        target.position = start;
    }

    public bool UpdateTween(float deltaTime)
    {
        elapsedTime += deltaTime;
        float t = Mathf.Clamp01(elapsedTime / duration);

        target.position = Vector3.Lerp(startPos, endPos, t);

        return (t >= 1f);
    }
}
