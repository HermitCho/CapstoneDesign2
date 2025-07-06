using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingRunnerController : MonoBehaviour
{

    [SerializeField] private Animator animator;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(TriggerFallAfterDelay());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator TriggerFallAfterDelay()
    {
        yield return new WaitForSeconds(2.5f);
        animator.SetTrigger("Fall");
    }

    
}
