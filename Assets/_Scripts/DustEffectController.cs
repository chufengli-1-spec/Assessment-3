using UnityEngine;

public class DustEffectController : MonoBehaviour
{
    [Header("Dust Effect Settings")]
    public ParticleSystem dustParticleSystem;
    public float emissionRate = 10f;
    public float dustOffset = -0.2f;
    
    private PacStudentController pacStudent;
    private ParticleSystem.EmissionModule emissionModule;
    private Vector3 lastPosition;
    private bool wasMoving = false;
    private bool isMoving = false; 

    void Start()
    {
        pacStudent = GetComponent<PacStudentController>();
        
        if (dustParticleSystem == null)
        {
            dustParticleSystem = GetComponentInChildren<ParticleSystem>();
        }
        
        if (dustParticleSystem != null)
        {
            emissionModule = dustParticleSystem.emission;
            emissionModule.rateOverTime = 0f; 
            
            dustParticleSystem.transform.localPosition = new Vector3(0, dustOffset, 0);
        }
        
        lastPosition = transform.position;
    }

    void Update()
    {
        if (dustParticleSystem == null || pacStudent == null) return;
        
        // 修复：使用属性而不是方法调用
        isMoving = pacStudent.IsMoving; // 移除括号 ()
        Vector3 currentPosition = transform.position;
        
        if (isMoving && !wasMoving)
        {
            EnableDustEffect();
        }
        else if (!isMoving && wasMoving)
        {
            DisableDustEffect();
        }
        
        if (isMoving)
        {
            UpdateDustDirection();
        }
        
        wasMoving = isMoving;
        lastPosition = currentPosition;
    }

    private void EnableDustEffect()
    {
        if (dustParticleSystem == null) return;
        
        emissionModule.rateOverTime = emissionRate;
        
        if (!dustParticleSystem.isPlaying)
        {
            dustParticleSystem.Play();
        }
    }

    private void DisableDustEffect()
    {
        if (dustParticleSystem == null) return;
        
        emissionModule.rateOverTime = 0f;
    }

    private void UpdateDustDirection()
    {
        if (dustParticleSystem == null || pacStudent == null) return;
        
        KeyCode currentDirection = pacStudent.GetCurrentDirection();
        Vector3 dustPosition = Vector3.zero;
        float rotationZ = 0f;
        
        switch (currentDirection)
        {
            case KeyCode.W: 
                dustPosition = new Vector3(0, dustOffset, 0);
                rotationZ = 0f;
                break;
            case KeyCode.S: 
                dustPosition = new Vector3(0, -dustOffset, 0);
                rotationZ = 180f;
                break;
            case KeyCode.A: 
                dustPosition = new Vector3(-dustOffset, 0, 0);
                rotationZ = 90f;
                break;
            case KeyCode.D: 
                dustPosition = new Vector3(dustOffset, 0, 0);
                rotationZ = -90f;
                break;
        }
        
        dustParticleSystem.transform.localPosition = dustPosition;
        dustParticleSystem.transform.localEulerAngles = new Vector3(0, 0, rotationZ);
    }

    public void ForceEnableDust()
    {
        EnableDustEffect();
    }

    public void ForceDisableDust()
    {
        DisableDustEffect();
    }

    public void SetEmissionRate(float rate)
    {
        emissionRate = rate;
        if (isMoving)
        {
            emissionModule.rateOverTime = emissionRate;
        }
    }
}