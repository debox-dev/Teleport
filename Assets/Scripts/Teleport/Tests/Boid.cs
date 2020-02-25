using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    [SerializeField]
    private Vector3 _flockPivot = Vector3.zero;

    [SerializeField]
    private float _maxFlockDistance = 20;

    [SerializeField]
    private float _rotationSpeed = 200;

    [SerializeField]
    private float _speed = 100;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var deltaToDistance = _flockPivot - transform.position;
        if (deltaToDistance.magnitude > _maxFlockDistance)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(deltaToDistance, Vector3.up), Time.deltaTime * _rotationSpeed);
        }
        transform.position += transform.forward * Time.deltaTime * _speed;
    }
}

