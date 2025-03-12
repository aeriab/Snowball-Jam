using System.Collections;
using UnityEngine;
using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class ballMovement : MonoBehaviour
{
    [SerializeField] private Material targetMaterial;
    [SerializeField] GameObject displayObject;
    [SerializeField] private GameObject allParticles;
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem dashParticles;
    [SerializeField] private ParticleSystem poundParticles;
    [SerializeField] private GameObject dashParticlesObject;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;


    [SerializeField] GameObject[] dashesFullObjects;
    [SerializeField] GameObject[] dashesEmptyObjects;
    [SerializeField] GameObject[] jumpsFullObjects;
    [SerializeField] GameObject[] jumpsEmptyObjects;

    [SerializeField] GameObject cameraObject;
    [SerializeField] GameObject ballObject;
    [SerializeField] Rigidbody rb;
    [SerializeField] GameObject touchGObject;
    [SerializeField] ParticleSystem endParticles;


    private float growthFactor = 0.06f;
    public float maxScale = 5.0f;
    public float minScale = 0.3f;
    public float camDistScale = 8.0f;

    public float forceMagnitude = 500.0f;
    public float speedyForceMag = 1000.0f;
    public float normalForceMag = 500.0f;
    public float baseMass;
    public float massScaling;


    private Terrain terrain; // Assign the terrain in the Inspector
    
    private TerrainData terrainData;
    private Vector3 terrainPosition;
    public int terrainLayerIndex = 0;
    private bool firstEndEmit = true;

    private float speed;
    private float scaleIncrease;
    private Vector3 newScale;

    private Vector3 startPosition;
    private Vector3 startScale;
    private MeshRenderer meshRenderer;
    public float jumpVelocity = 35f;
    public float dashVelocity = 45f;
    public float gravityMultiplier = 2f;
    
    public float baseAcceleration = 1.3f;

    private float numJumps = 0.1f;
    public float jumpRecharge = 1f;
    public float jumpsAllowed = 1f;

    private float numDashes = 0.1f;
    public float dashRecharge = 1f;
    public float dashesAllowed = 1f;

    public float score = 0.0f;
    private float highScore = 0.0f;
    public float maxRBSubstitute = 40.0f;
    
    private Leaderboard leaderboard;

    public AudioSource audioSource; // Reference to AudioSource
    public AudioClip soundEffectDash;
    public AudioClip soundEffectJump;
    public AudioClip soundEffectPound;
    public AudioClip soundEffectKill;
    public AudioClip soundEffectDie;

    PhotonView view;

    private bool colorHasBeenEstablished = false;



    public void setColor(Vector3 vecColor) {
        Renderer renderer = GetComponent<Renderer>();
        Material targetMaterialInstance = new Material(targetMaterial); // Create an instance of the material
        targetMaterialInstance.SetColor("_Color", new Color(vecColor.x, vecColor.y, vecColor.z));
        renderer.material = targetMaterialInstance;
    }

    public void establishColor() {
        view = GetComponent<PhotonView>();

        if (view.IsMine)
        {
            // Debug.Log("The view was mine :p");
            var playerData = PhotonNetwork.LocalPlayer.CustomProperties;
            if (playerData.ContainsKey("Nickname"))
            {
                string nickname = (string)playerData["Nickname"];
                gameObject.name = nickname;
            }
            if (playerData.ContainsKey("Color"))
            {
                Vector3 colorVector = (Vector3)playerData["Color"];

                Renderer renderer = GetComponent<Renderer>();
                Material targetMaterialInstance = new Material(targetMaterial); // Create an instance of the material
                targetMaterialInstance.SetColor("_Color", new Color(colorVector.x, colorVector.y, colorVector.z));
                renderer.material = targetMaterialInstance;

                // targetMaterial.SetColor("_Color", new Color(colorVector.x, colorVector.y, colorVector.z));
                // Debug.Log("Vector stuff I threw together: " + colorVector.x + ", " + colorVector.y + ", " + colorVector.z);
                colorHasBeenEstablished = true;
            } else {
                Debug.Log("no color key :(");
            }
        } else {

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
        view = GetComponent<PhotonView>();

        // if (!GetComponent<PhotonView>().IsMine)
        // {
        //     rb.isKinematic = true; // Prevent physics conflicts for non-owner
        // }


        GameObject leaderboardObject = GameObject.Find("LeaderboardLabel");
        if (leaderboardObject != null)
        {
            leaderboard = leaderboardObject.GetComponent<Leaderboard>();
            if (leaderboard != null)
            {
                // Debug.Log("Leaderboard component found!");
            }
            else
            {
                Debug.LogWarning("Leaderboard component not found on LeaderboardLabel.");
            }
        }

        GameObject terrainObject = GameObject.Find("SmallTerrain");
        if (terrainObject != null)
        {
            terrain = terrainObject.GetComponent<Terrain>();
        }


        startPosition = transform.position;
        startScale = transform.localScale;
        if (terrain != null)
        {
            terrainData = terrain.terrainData;
            terrainPosition = terrain.GetPosition(); // Terrain world position
        }
        else
        {
            Debug.LogError("Please assign the terrain in the Inspector.");
        }
    }

    // [PunRPC]
    // private void SyncMaterialColor(string colorString)
    // {
    //     if (ColorUtility.TryParseHtmlString($"#{colorString}", out Color color))
    //     {
    //         objectRenderer.material.color = color;
    //     }
    // }

    // Update is called once per frame
    void Update()
    {
        if (view.IsMine) {
            if(!colorHasBeenEstablished) {
                establishColor();
            }
        }
        




        if (!view.IsMine) {
            ballMovement playerCommunication = GetComponent<ballMovement>();
        }
        
        if (view.IsMine) {

            score = rb.transform.localScale.magnitude * 100f;
            // leaderboard.scores[10] = (int)score;

            allParticles.transform.localScale = rb.transform.localScale;
            jumpParticles.transform.localScale = rb.transform.localScale;
            dashParticles.transform.localScale = rb.transform.localScale;
            poundParticles.transform.localScale = rb.transform.localScale;

            
            if (score > highScore) {
                highScore = score;
            }
            scoreText.text = "SCORE: " + (int)score;
            highScoreText.text = "HIGHSCORE: " + (int)highScore;


            growthFactor = 0.025f / (1f + 0.5f * rb.transform.localScale.magnitude);

            if (Input.GetKeyDown(KeyCode.Space) && numJumps > 0f) {
                if (rb != null) {
                    audioSource.PlayOneShot(soundEffectJump);
                    Vector3 velocity = rb.velocity;
                    velocity.y = jumpVelocity * (1f + Mathf.Min(maxRBSubstitute,rb.transform.localScale.magnitude) * 0.03f);
                    rb.velocity = velocity;
                    jumpParticles.Play();
                    numJumps -= 1f;
                }
            }

            int jumpsIndex = 0;
            foreach (GameObject obj in jumpsFullObjects) {
                if (obj != null) {
                    if (numJumps <= jumpsIndex) {
                        obj.SetActive(false);
                    } else {
                        obj.SetActive(true);
                    }
                    if (jumpsAllowed <= jumpsIndex) {
                        obj.SetActive(false);
                    }
                }
                jumpsIndex += 1;
            }
            int emptyJumpsIndex = 0;
            foreach (GameObject obj in jumpsEmptyObjects) {
                if (obj != null) {
                    if (jumpsAllowed <= emptyJumpsIndex) {
                        obj.SetActive(false);
                    } else {
                        obj.SetActive(true);
                    }
                }
                emptyJumpsIndex += 1;
            }

            



            if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) && numDashes > 0f) {
                if (rb != null) {
                    audioSource.PlayOneShot(soundEffectDash);
                    cameraMovement camScript = cameraObject.GetComponent<cameraMovement>();
                    float direction = camScript.currentRotation;
                    Vector3 vecDirection = new Vector3((float)Math.Sin((direction/360.0) * (2 * Math.PI)),0,(float)Math.Cos((direction/360.0) * (2 * Math.PI)));
                    Vector3 velocity = 4.0f * vecDirection * dashVelocity * (10f + rb.transform.localScale.magnitude) * 0.03f;
                    rb.velocity = velocity;
                    dashParticlesObject.transform.rotation = Quaternion.LookRotation(-vecDirection);
                    dashParticles.Play();
                    numDashes -= 1f;
                }
            }
            int dashesIndex = 0;
            foreach (GameObject obj in dashesFullObjects) {
                if (obj != null) {
                    if (numDashes <= dashesIndex) {
                        obj.SetActive(false);
                    } else {
                        obj.SetActive(true);
                    }
                    if (dashesAllowed <= dashesIndex) {
                        obj.SetActive(false);
                    }
                }
                dashesIndex += 1;
            }
            int emptyDashesIndex = 0;
            foreach (GameObject obj in dashesEmptyObjects) {
                if (obj != null) {
                    if (dashesAllowed <= emptyDashesIndex) {
                        obj.SetActive(false);
                    } else {
                        obj.SetActive(true);
                    }
                }
                emptyDashesIndex += 1;
            }




            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) {
                if (rb != null) {
                    audioSource.PlayOneShot(soundEffectPound);
                    Vector3 vecDirection = new Vector3(0,-1, 0);
                    Vector3 velocity = 4.0f * vecDirection * dashVelocity * 1.2f * (10f + rb.transform.localScale.magnitude) * 0.03f;
                    poundParticles.Play();
                    rb.velocity = velocity;
                }
            }








            if (terrainData != null)
            {
                Vector3 ballPosition = transform.position;

                // Get the terrain's local coordinates
                float relativeX = (ballPosition.x - terrainPosition.x) / terrainData.size.x;
                float relativeZ = (ballPosition.z - terrainPosition.z) / terrainData.size.z;

                // Get the corresponding texture index
                terrainLayerIndex = GetTerrainLayerAtPosition(relativeX, relativeZ);

                // Debug.Log("Current Terrain Layer Index: " + terrainLayerIndex);
            }
        } else {
            rb.isKinematic = true;
        }
    }

    private void FixedUpdate() {
        if (view.IsMine) {

            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

            if (touchGObject != null) {
                touchingGround tgScript = touchGObject.GetComponent<touchingGround>();
                cameraMovement camScript = cameraObject.GetComponent<cameraMovement>();
                if (tgScript != null) {
                    camScript.camDistance = 10.0f + rb.transform.localScale.magnitude * camDistScale;
                    camScript.cameraHeightOffset = camScript.camDistance * (4.553655f/10.0f);
                    if (tgScript.isGrounded) {
                        numJumps += Time.deltaTime * jumpRecharge;
                        numJumps = Mathf.Min(jumpsAllowed - 0.9f,numJumps);
                        numDashes += Time.deltaTime * dashRecharge;
                        numDashes = Mathf.Min(dashesAllowed - 0.9f,numDashes);

                        float direction = camScript.currentRotation;
                        Vector3 vecDirection = new Vector3((float)Math.Sin((direction/360.0) * (2 * Math.PI)),0,(float)Math.Cos((direction/360.0) * (2 * Math.PI)));
                        rb.AddForce(vecDirection * baseAcceleration, ForceMode.Acceleration);

                        // rb.AddForce((float)(Time.deltaTime * forceMagnitude * (Mathf.Pow(rb.mass,3)) * Input.GetAxis("Vertical") * Math.Sin((direction/360.0) * (2 * Math.PI))),0,(float)(Time.deltaTime * (forceMagnitude * (Mathf.Pow(rb.mass,3))) * Input.GetAxis("Vertical") * Math.Cos((direction/360.0) * (2 * Math.PI))));
                        speed = rb.velocity.magnitude;
                        scaleIncrease = speed * growthFactor * Time.deltaTime;
                        newScale = rb.transform.localScale + Vector3.one * scaleIncrease;
                        newScale = Vector3.Min(newScale, Vector3.one * maxScale);
                        rb.mass = baseMass + massScaling * Mathf.Pow(newScale.magnitude, 3);
                        rb.transform.localScale = newScale;
                        forceMagnitude = normalForceMag;

                        // if (tgScript.onSnow || terrainLayerIndex == 1 || terrainLayerIndex == 2) {
                            
                        // } else {
                        //     speed = rb.velocity.magnitude;
                        //     scaleIncrease = -speed * growthFactor * 2.5f * Time.deltaTime;
                        //     newScale = rb.transform.localScale + Vector3.one * scaleIncrease;
                        //     newScale = Vector3.Max(newScale, Vector3.one * minScale);
                        //     rb.mass = baseMass + massScaling * Mathf.Pow(newScale.magnitude, 3);
                        //     rb.transform.localScale = newScale;
                        // }

                        // if (terrainLayerIndex == 2) {
                        //     forceMagnitude = speedyForceMag;
                        // } else {
                        //     forceMagnitude = normalForceMag;
                        // }
                    }
                    scaleIncrease = -growthFactor * 3f * Time.deltaTime;
                    newScale = rb.transform.localScale + Vector3.one * scaleIncrease;
                    newScale = Vector3.Max(newScale, Vector3.one * minScale);
                    rb.mass = baseMass + massScaling * Mathf.Pow(newScale.magnitude, 3);
                    rb.transform.localScale = newScale;
                }
            }
            if (rb.transform.localScale.magnitude <= (Vector3.one * minScale).magnitude) {
                StartCoroutine(EmitParticlesAndReset());
            }
        }
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send the current state to other players
            stream.SendNext(transform.position);
            stream.SendNext(rb.velocity);
            stream.SendNext(rb.mass);
            stream.SendNext(transform.localScale);
        }
        else
        {
            // Receive state updates from other players
            transform.position = (Vector3)stream.ReceiveNext();
            rb.velocity = (Vector3)stream.ReceiveNext();
            rb.mass = (float)stream.ReceiveNext();
            transform.localScale = (Vector3)stream.ReceiveNext();
        }
    }


    public void increaseScale(Vector3 scaleIncrease) {
        rb.transform.localScale += scaleIncrease;
    }

    public void KillPlayer() {
        jumpRecharge = 1f;
        dashRecharge = 1f;
        numJumps = 0.1f;
        numDashes = 0.1f;
        dashVelocity = 35f;
        jumpVelocity = 45f;
        dashesAllowed = 1f;
        jumpsAllowed = 1f;

        StartCoroutine(EmitParticlesAndReset());
    }

    public void addUpgrades(int upgradeNum) {
        audioSource.PlayOneShot(soundEffectKill);
        int upIndex = 0;
        DisplayNotification displayUpgrade = displayObject.GetComponent<DisplayNotification>();
        while (upIndex < upgradeNum) {
            float randNum = UnityEngine.Random.Range(0.0f,8.0f);
            if (randNum < 1.0f) {
                jumpRecharge += 0.2f;
                displayUpgrade.display("+1 jump recharge");
            } else if (randNum < 2.0f) {
                dashRecharge += 0.2f;
                displayUpgrade.display("+1 dash recharge");
            } else if (randNum < 3.0f) {
                if (jumpsAllowed < 5f) {
                    jumpsAllowed += 1f;
                } else {
                    upIndex -= 1;
                }
                displayUpgrade.display("+1 jump");
            } else if (randNum < 4.0f) {
                if (dashesAllowed < 5f) {
                    dashesAllowed += 1f;
                } else {
                    upIndex -= 1;
                }
                displayUpgrade.display("+1 dash");
            } else if (randNum < 5.0f) {
                displayUpgrade.display("+1 dash velocity");
            } else if (randNum < 6.0f) {
                displayUpgrade.display("+1 jump velocity");
            } else if (randNum < 7.0f) {
                newScale = rb.transform.localScale + Vector3.one * 1.5f;
                newScale = Vector3.Min(newScale, Vector3.one * maxScale);
                rb.mass = baseMass + massScaling * Mathf.Pow(newScale.magnitude, 3);
                rb.transform.localScale = newScale;
                displayUpgrade.display("+1 SIZE");
            } else if (randNum < 8.0f) {
                baseAcceleration += 1f;
                displayUpgrade.display("+1 speed");
            }
            // Debug.Log(randNum);
            upIndex += 1;
        }
    }

    public IEnumerator EmitParticlesAndReset() {
        if (endParticles != null && firstEndEmit) {
            endParticles.Play();
            audioSource.PlayOneShot(soundEffectDie);
            firstEndEmit = false;
            meshRenderer = ballObject.GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;
            Collider colliderOfInterest = ballObject.GetComponent<Collider>();
            colliderOfInterest.enabled = false;
            rb.useGravity = false;
        }
        yield return new WaitForSeconds(2.0f);
        if (!firstEndEmit) {
            startPosition = newRandomPos();
            transform.position = startPosition;
            // Debug.Log("START POSITION: " + startPosition);
            transform.localScale = startScale;
            meshRenderer = ballObject.GetComponent<MeshRenderer>();
            meshRenderer.enabled = true;
            Collider colliderOfInterest = ballObject.GetComponent<Collider>();
            colliderOfInterest.enabled = true;
            rb.useGravity = true;
            firstEndEmit = true;
        }
        
    }

    public float minX;
    public float maxX;
    public float minZ;
    public float maxZ;
    public float baseY;

    public Vector3 newRandomPos() {
        return new Vector3(UnityEngine.Random.Range(minX, maxX), baseY, UnityEngine.Random.Range(minZ, maxZ));
    }


    int GetTerrainLayerAtPosition(float x, float z)
    {
        // Convert normalized coordinates to TerrainData resolution
        int mapX = Mathf.Clamp((int)(x * terrainData.alphamapWidth), 0, terrainData.alphamapWidth - 1);
        int mapZ = Mathf.Clamp((int)(z * terrainData.alphamapHeight), 0, terrainData.alphamapHeight - 1);

        // Get the terrain layer weights at this point
        float[,,] alphaMap = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);
        float[] layerWeights = new float[alphaMap.GetLength(2)];

        for (int i = 0; i < layerWeights.Length; i++)
        {
            layerWeights[i] = alphaMap[0, 0, i];
        }

        // Find the layer with the maximum weight
        int dominantLayer = 0;
        float maxWeight = 0;

        for (int i = 0; i < layerWeights.Length; i++)
        {
            if (layerWeights[i] > maxWeight)
            {
                maxWeight = layerWeights[i];
                dominantLayer = i;
            }
        }

        return dominantLayer; // Return the index of the dominant terrain layer
    }
}
