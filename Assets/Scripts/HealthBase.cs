using System;
using UnityEngine;

public class HealthBase : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 10; 
    private float _currentHealth;
    [Range(0, 1)][SerializeField] private float _resistance = 0f;

    private void Start()
    {
        _currentHealth = _maxHealth;
    }

    public void Damage(float damage)
    {
        if (_currentHealth - damage <= 0)
        {
            _currentHealth = 0;
            DeathSequence();
        }
        else
            _currentHealth -= damage * (1-_resistance);
    }

    public void Heal(float heal)
    {
        if (_currentHealth + heal >= _maxHealth)
            _currentHealth = _maxHealth;
        else
            _currentHealth += heal;
    }

    protected virtual void DeathSequence()
    {
        Debug.Log("Man i'm ded");
    }
}
