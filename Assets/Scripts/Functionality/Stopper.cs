using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stopper : MonoBehaviour
{
    [SerializeField]
    private BonusController _controller;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_controller.isCollision)
        {
            _controller.isCollision = true;
            Debug.Log("collision done");
            _controller.StopWheel();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!_controller.isCollision)
        {
            _controller.isCollision = true;
            Debug.Log("collision done");
            _controller.StopWheel();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!_controller.isCollision)
        {
            _controller.isCollision = true;
            Debug.Log("collision done");
            _controller.StopWheel();
        }
    }
}
