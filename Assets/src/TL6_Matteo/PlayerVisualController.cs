using UnityEngine;
using TwoHandedHero;

public class PlayerVisualController : MonoBehaviour
{
    [SerializeField] private TwoHandedRig _rig;
    [SerializeField] private PlayerControllerTL6 _controller;
    [SerializeField] private Rigidbody2D _rb;


    public void LateUpdate()
    {
        _rig.SetFacingRight(_controller.FacingDirection > 0);

        bool isMoving = Mathf.Abs(_rb.linearVelocity.x) > 0.1f;
        _rig.SetMoving(isMoving);
    }


}
