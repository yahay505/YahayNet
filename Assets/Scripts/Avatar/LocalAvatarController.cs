using Network.Physics;
using UnityEngine;
using static UnityTools.EasyVec;
namespace Avatar
{
    public partial class AvatarController
    {
        

        private void Local_FixedUpdate()
        {

        }

        void Local_Update()
        {
            updateVelocity();

            updateCamRotation();
 
            updateJump();


            if (false)
            {
                    //hold item 
                    
                    // get ownership
                
            }
        }

        [SerializeField] private float jumpSpeed;
        private float coyoteTime=1f, coyoteCounter;
        private float jumpbufferTime = 0.2f,jumpBufferCounter;
        private void updateJump()
        {
            if (isGrounded())
            {
                coyoteCounter = coyoteTime;
            }
            else
            {
                coyoteCounter -= Time.deltaTime;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                jumpBufferCounter = jumpbufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            if (jumpBufferCounter>0&&coyoteCounter>0)
            {
                jumpBufferCounter = 0;
                GetComponent<Rigidbody>().velocity = vec(GetComponent<Rigidbody>().velocity.x, jumpSpeed, GetComponent<Rigidbody>().velocity.z);
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (GetComponent<Rigidbody>().velocity.y>0)
                {
                    GetComponent<Rigidbody>().velocity -= vec(0, GetComponent<Rigidbody>().velocity.y / 2, 0);
                }

                coyoteCounter = 0;
            }

            
            
            
            bool isGrounded()
            {
                return true;
            }
        }

        #region move&look

            

            private void updateVelocity()
            {
                Vector3 newVelocity = Vector3.zero;
                var y = rb.velocity.y;
                newVelocity.x = 0;
                newVelocity.z = 0;
                if (Input.GetKey(KeyCode.W))
                {
                    newVelocity += transform.forward * speed;
                }

                if (Input.GetKey(KeyCode.A))
                {
                    newVelocity += transform.right * -speed;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    newVelocity += transform.forward * -speed;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    newVelocity += transform.right * speed;
                }

                newVelocity.y = y;
                rb.velocity = newVelocity;
            }


            [Range(0.1f, 9f)][SerializeField] float sensitivity = 2f;
            [Tooltip("Limits vertical camera rotation. Prevents the flipping that happens when rotation goes above 90.")]
            [Range(0f, 90f)][SerializeField] float yRotationLimit = 88f;

            Vector2 rotation = Vector2.zero;
            const string xAxis = "Mouse X"; //Strings in direct code generate garbage, storing and re-using them creates no garbage
            const string yAxis = "Mouse Y";
            private void updateCamRotation()
            {

                
                rotation.x += Input.GetAxis(xAxis) * sensitivity;
                rotation.y += Input.GetAxis(yAxis) * sensitivity;
                rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);
                var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
                var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);

                Camera.main!.transform.localRotation =  yQuat; //Quaternions seem to rotate more consistently than EulerAngles. Sensitivity seemed to change slightly at certain degrees using Euler. transform.localEulerAngles = new Vector3(-rotation.y, rotation.x, 0);
                transform.localRotation = xQuat;
            }
        #endregion

        
        private void Local_OnDrawGizmos()
        {
            
            Gizmos.color= Color.cyan;
            Gizmos.DrawRay(transform.position,transform.forward);
            Gizmos.color= Color.red;
            Gizmos.DrawRay(transform.position,transform.right);
            // Gizmos.color=Color.magenta;
            // Gizmos.DrawRay(transform.forward,
            //     transform.forward - vec(0, Vector3.Dot(transform.forward, Vector3.up), 0));
            // Gizmos.DrawRay(transform.right,
            //     transform.right - vec(0, Vector3.Dot(transform.right, Vector3.up), 0));
        }
    }
}