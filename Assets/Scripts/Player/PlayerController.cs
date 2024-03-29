﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
	private Rigidbody rb;

	[SerializeField]
	private Camera cam;
	private const float speed = 1f;
	private const float lookSensitivity = 1f;
	private const float viewRange = 65.0f;

	[SerializeField]
	private const float deathRadius = 5f;
	private const float cartRegenRadius = 1.5f;

	public float playerFuel;

	private float rotY = 0;
	private float rotX = 0;

	public AudioSource ignitor;
	public AudioSource extinguisher;
	public AudioSource flame;
	public AudioSource ignition;
	public AudioSource spook;
	public AudioSource wind;

	public ParticleSystem sparks;

	private bool lanternOn = false;
	private int ambiantOnly = 0;
	public int remainingLightAttempts;

	public float burnRate;
	public Transform needle;

	public Light mainLight;
	public Light ambiantLight;

	private GameObject cart;

	public float deathTimer;
	private bool dead = false;
	public bool mortal = false;
	[SerializeField]
	private float health = 1f;
	public float healthDepletionRate;
	public float cartRegenRate;
	public float lanternRegenRate;

	private float windTimer = 20f;

	// Start is called before the first frame update
	void Start()
    {

    	Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();

        cart = GameObject.Find("Cart");
    }

	private void Update()
	{

		//Look and Interact
		if (Input.GetButtonDown("Fire1"))
		{
			RaycastHit hit;

			if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 1000))
			{
				GameObject hitObject = hit.collider.GetComponentInParent<Transform>().gameObject;

				Debug.Log("(Down) Hit object: " + hitObject.name);

				if (hitObject.tag == "FuelCell")
				{

					FuelCellController cell = hitObject.GetComponent<FuelCellController>();

					playerFuel += cell.fuelAmount;
					if (playerFuel > 1f)
					{
						playerFuel = 1f;
					}

					cell.Consume();
				}
				else if (hitObject.tag == "SodaStream")
				{

				}
				else if (hitObject.name == "Cart")
				{
					cart.GetComponent<CartController>().StartCart();
				}

			}
		}
		else if (Input.GetButton("Fire1"))
		{
			RaycastHit hit;

			if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 1000))
			{
				GameObject hitObject = hit.collider.GetComponentInParent<Transform>().gameObject;

				Debug.Log("(Held) Hit object: " + hitObject.name);

				if (hitObject.tag == "SodaStream")
				{
					

					if (cart.GetComponent<CartController>().cartFuel < 1f)
					{
						hitObject.GetComponent<AudioSource>().Play();

						float transferAmount = Time.deltaTime / 5f;

						if (playerFuel < transferAmount)
						{
							transferAmount = playerFuel;
						}

						playerFuel -= transferAmount;

						cart.GetComponent<CartController>().cartFuel += transferAmount;

						if (cart.GetComponent<CartController>().cartFuel > 1f)
						{
							cart.GetComponent<CartController>().cartFuel = 1f;
						}
					}
					
				}
				else
				{
					Debug.Log("Fire 1 down not hit soda hit something");
				}
			}
		}
		else
		{
			RaycastHit hit;

			Debug.DrawRay(cam.transform.position, cam.transform.forward, Color.green);

			if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 1000))
			{
				GameObject hitObject = hit.collider.GetComponentInParent<Transform>().gameObject;

				if (hitObject.tag == "FuelCell")
				{
					int onOff = Random.Range(0, 2);

					if (onOff == 0)
					{
						needle.GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");
					}
					else
					{
						needle.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
					}
					
				}
				else if (hitObject.tag == "SodaStream")
				{

				}
				else
				{
					needle.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
				}
			}
		}

		//Lantern
		if (Input.GetButtonDown("Fire2"))
		{

			if (remainingLightAttempts < 0)
			{
				remainingLightAttempts = 0;
			}

			if (lanternOn)
			{
				lanternOn = false;
				flame.Stop();
				extinguisher.Play();
			}
			else
			{
				ignitor.Play();

				//Burst particles
				sparks.Clear();
				sparks.Play();

				if (remainingLightAttempts == 0 && playerFuel > 0f && !dead)
				{
					ignition.Play();
					flame.Play();

					lanternOn = true;
					remainingLightAttempts = Random.Range(0, 5);
				}
				else
				{
					remainingLightAttempts -= 1;
					if (remainingLightAttempts < 0)
					{
						remainingLightAttempts = 0;
					}
					ambiantOnly = Random.Range(4, 9);
				}

			}
		}
	}

	// Update is called once per frame
	void FixedUpdate()
    {
		//Walking Audio
		if (Input.GetKeyDown(KeyCode.W))
		{
			GetComponent<AudioSource>().Play();
		}
		else if (Input.GetKeyUp(KeyCode.W))
		{
			GetComponent<AudioSource>().Stop();
		}
		else if (!Input.GetKey(KeyCode.W))
		{
			GetComponent<AudioSource>().Stop();
		}

		//Calculate movement velocity as a 3D vector
		float xMov = Input.GetAxisRaw("Horizontal");
        float zMov = Input.GetAxisRaw("Vertical");

        Vector3 moveHorizontal = transform.right * xMov;
        Vector3 moveVertical = transform.forward * zMov;

        Vector3 velocity = (moveHorizontal + moveVertical).normalized * speed;

        if(velocity != Vector3.zero)
        {
        	rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }

        //Calculate rotation as a 3D vector and apply
        rotX += Input.GetAxis("Mouse X") * lookSensitivity;

        //This is necessary
        transform.rotation = Quaternion.Euler(0f, rotX, 0f);

		rotY += Input.GetAxis("Mouse Y") * lookSensitivity;
  		rotY = Mathf.Clamp(rotY, -viewRange, viewRange);
   		cam.transform.rotation = Quaternion.Euler(-rotY, rotX, 0f);


		//===============Lantern=================

		//===============Fuel===============
		if (lanternOn)
		{
			playerFuel -= burnRate * Time.deltaTime;

			if (playerFuel <= 0f)
			{
				lanternOn = false;
				playerFuel = 0f;
				flame.Stop();
				extinguisher.Play();
			}
		}
		//===============/Fuel===============

		//=============Needle=============
		float targetRot = playerFuel * 320f;

		//float currentRotation = -needle.rotation.eulerAngles.z;
		//float delta = Mathf.Abs(1/(targetRot - currentRotation));
		//float newRotation = (Mathf.Pow((float)System.Math.E, -delta) * Mathf.Sin(delta)) + currentRotation;
		//needle.rotation = Quaternion.Euler(new Vector3(needle.rotation.eulerAngles.x, needle.rotation.eulerAngles.y, -newRotation));
		//Debug.Log("Fuel: " + playerFuel + " Target: " + targetRot + " Current: " + currentRotation + " Delta: " + delta	+ " Final Rotation: " + needle.rotation.eulerAngles.z);

		needle.rotation = Quaternion.Euler(new Vector3(needle.rotation.eulerAngles.x, needle.rotation.eulerAngles.y, -targetRot));
		//=============/Needle=============

		//=============Light Controll=============
		//Enabling the lights when lanternOn is true
		mainLight.enabled = lanternOn;
		ambiantLight.enabled = lanternOn;

		//allows the ambient light to be on by itself for ambientOnly number of frames (used for sparking)
		if (!lanternOn && ambiantOnly > 0)
		{
			ambiantLight.enabled = true;
			ambiantOnly -= 1;
		}
		else if (lanternOn && ambiantOnly > 0)
		{
			ambiantOnly = 0;
		}
		//=============/Light Controll=============

		//=============Wind=============

		if (windTimer <= 0f)
		{
			windTimer = Random.Range(30, 300);
			AudioSource.PlayClipAtPoint(wind.clip, new Vector3(transform.position.x - Random.Range(5, 10), transform.position.y, transform.position.z - Random.Range(5, 10)), 1f);
			if (lanternOn)
			{
				lanternOn = false;
				flame.Stop();
				extinguisher.Play();
			}
		}
		else
		{
			windTimer -= Time.deltaTime;
		}

		//=============/Wind=============

		//===============/Lantern==============

		//=============Death===============
		if (mortal && !dead)
		{
			if (!lanternOn && Vector3.Distance(cart.transform.position, transform.position) > deathRadius)
			{
				if (health <= 0f)
				{
					//Die
					dead = true;
					spook.volume = 1f;
					spook.Play();
					//Play death sound

					
				}
				else
				{
					float oldHealth = health;
					health -= healthDepletionRate * Time.deltaTime;

					if (oldHealth > 0.8f && health <= 0.8f)
					{
						spook.volume = 0.05f;
						spook.Play();
					}
					else if (oldHealth > 0.6f && health <= 0.6f)
					{
						spook.volume = 0.1f;
						spook.Play();
					}
					else if (oldHealth > 0.4f && health <= 0.4f)
					{
						spook.volume = 0.2f;
						spook.Play();
					}
					else if (oldHealth > 0.2f && health <= 0.2f)
					{
						spook.volume = 0.4f;
						spook.Play();
					}
				}
			}
			else if (Vector3.Distance(cart.transform.position, transform.position) < cartRegenRadius)
			{
				health += cartRegenRate * Time.deltaTime;
				
			}
			else if (lanternOn)
			{
				health += lanternRegenRate * Time.deltaTime;
			}

			if (health > 1f)
			{
				health = 1f;
			}
		}
		else if (mortal && dead)
		{
			deathTimer -= Time.deltaTime;

			if (deathTimer <= 0f)
			{
				//Go back to main menu scene
				SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
			}
		}
		//=============/Death===============
	}
}