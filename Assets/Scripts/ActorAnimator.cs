using UnityEngine;

public class ActorAnimator : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    public void PlaySuccess()
    {
        _animator.SetTrigger("Success");
    }

    public void PlayFail()
    {
        _animator.SetTrigger("Fail");
    }

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = gameObject.GetComponent<Animator>();
        }
    }
}
