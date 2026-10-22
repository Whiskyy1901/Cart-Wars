using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float _traumaDecay = 1.5f;
    [SerializeField] private float _maxPitchShake = 4f;
    [SerializeField] private float _maxRollShake = 3f;
    [SerializeField] private float _maxYawShake =2f;
    [SerializeField] private float _noiseFrequency = 25f;

    private  float _trauma;
    private float _seed;

    private void Awake()
    {
        _seed = Random.Range(0f, 100f);

    }

    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    public void LateUpdate()
    {
        if(_trauma <=0f)
        return;

        _trauma = Mathf.Max(0f, _trauma - _traumaDecay * Time.deltaTime);

        float ShakeAmount = _trauma * _trauma;

        float t = Time.time * _noiseFrequency;
        float pitch = (Mathf.PerlinNoise(_seed, t) * 2f - 1f)* _maxPitchShake * ShakeAmount;
        float yaw = (Mathf.PerlinNoise(_seed +1f, t)* 2f - 1f)*_maxYawShake * ShakeAmount;
        float roll = (Mathf.PerlinNoise(_seed +2f, t)*2f -1f)*_maxRollShake * ShakeAmount;

        transform.localRotation *= Quaternion.Euler(pitch, yaw, roll);
        
    }

}