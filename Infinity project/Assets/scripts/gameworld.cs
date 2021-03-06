using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gameworld : MonoBehaviour
{
	//objects that are referenced
	Grid grid;// world grid
	Node previewNode;//the node that the player will land on
	GameObject previewCube;	//the actual asset that is seen to mark where the player will land
	CameraControls cameraController; // the camera controller
	//
	//lists / arrays
	List<GameObject> selectedUnits;
	public GameObject[] availableUnits;
	public List<GameObject> battleLineUp = new List<GameObject>();
	//
	//layermasks
	public LayerMask unwalkableMask;
	public LayerMask playerMask;
	public LayerMask floorMask;
	public LayerMask npcMask;
	//
	// mouse use 
	bool dClick; // double click if true
	bool lMouseDown; // if the left mouse button if currently down!
	bool dragSelect; // if true mouse is now dragging and no longer clicking
	bool haveplayers; //boolean to control the preview when moving a player(check ongui)
	public float checkRadius;
	Vector3 mousePosition1; //mouse position 
	public float timeBetweenDClick; // standard is 100 to 900 miliseconds so use the numbers between .1 and .9 - standard is .5!
	public float minDragDist; // this is the minimum distance from two vectors for it to be considered a drag
	float lastTime; // last time the mouse was clicked -> used in double click
	bool lControlPressed;
	//
	//combat
	public float turnDelay = .1f;
	bool combatMode = false; // you may need to make this public
	bool playerTurn;
	private bool turnInProgress;
	private int currentTurn = 0;
	//

	void Awake ()
	{
		playerTurn = true;
		turnInProgress = false;
		floorMask = LayerMask.GetMask ("floor");
		playerMask = LayerMask.GetMask ("player");
		npcMask = LayerMask.GetMask ("Npc");
		haveplayers = false;
		previewCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		previewCube.transform.localScale = new Vector3 (1, 0.5f, 1);
		previewCube.SetActive (false);
		lastTime = Time.time;	
		grid = GameObject.Find("Pathfinding").GetComponent<Grid> ();
		selectedUnits = new List<GameObject> ();
		lControlPressed = new bool ();	
		dClick = false;
		lMouseDown = false;
		dragSelect = false;
		battleLineUp = new List<GameObject>();
		availableUnits = GameObject.FindGameObjectsWithTag ("Player");
		for (int i = 0; i < availableUnits.Length; i++) {
			battleLineUp.Add(availableUnits[i]);
		}
		SortCombatLineupUp(battleLineUp.Count-1);
		cameraController = GameObject.Find("PlayersView").GetComponent<CameraControls> ();
	}


	void Update ()
	{
												//THIS CONTROLS INTERACTION WITH THE GAME WORLD
		//check if left control is being pressed
		if (Input.GetKeyDown (KeyCode.LeftControl)) {

			lControlPressed = true;
		}
		if (Input.GetKeyUp (KeyCode.LeftControl)) {

			lControlPressed = false;
		}
		//Debug button
		if (Input.GetKeyUp (KeyCode.F)) {

			print ("combat mode = "+ combatMode+ " turn = "+ currentTurn);

		}
		//

		//check if mouse 1 is down
		if (Input.GetMouseButtonDown (0)) {
			// mouse down equals true
			lMouseDown = true;
			//check if left controlpressed is true if so dont do check for double click or drag
			if (!lControlPressed) {
				// get mouse position 1
				mousePosition1 = Input.mousePosition;
				//check if it has double clicked if not set up the double click
				if (lastTime + timeBetweenDClick > Time.time) {
					dClick = true;	

				} else {
					dClick = false;
					lastTime = Time.time;
				}

			}
		}
		if (lMouseDown && !lControlPressed) {
			// check if it has dragged past the min distance
			if (minDragDist <= Vector3.Distance (mousePosition1, Input.mousePosition)) {
				dragSelect = true;
			}
		}
		//check if mouse 1 is up
		if (Input.GetMouseButtonUp (0)) {
			// check if drag is the active -------------------------------------------------------------------combat mode check
			if (dragSelect && playerTurn ) {
				//do drag stuff
				//go through all players and see if they are in the view!
				selectedUnits.Clear();
				for (int i = 0; i < availableUnits.Length; i++) {
					if(IsWithinSelectionBounds( availableUnits[i])){
						selectedUnits.Add (availableUnits [i]);

					}
				}
			} else if(playerTurn ){
				// raycasting below
				Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
				RaycastHit hit;
				if (Physics.Raycast (ray, out hit)) {
					//if it hits a player
					if (Physics.CheckSphere (hit.point, checkRadius, playerMask)) {
						if (dClick) {
							//check if double click is the active
							//do double click stuff
							//print ("player double click ");
						} else if (lControlPressed) {
							//check if lftcntrl is the active
							//do lftcntrl stuff
							selectedUnits.Add (hit.transform.gameObject);

							//else
						} else {
							// single click is avtive
							// do single click stuff
							selectedUnits.Clear ();
							selectedUnits.Add (hit.transform.gameObject);
						}
					} 
					//if it hits an enemy
					if(Physics.CheckSphere(hit.point,checkRadius,npcMask)){
						for (int i = 0; i < selectedUnits.Count; i++) {
							//attack the Npc (only if an enemy)
							//print(Vector3.Distance(selectedUnits [i].transform.position, hit.transform.position) + " / "+ selectedUnits[i].GetComponent<UnitStats> ().range);
							if (MyTurnYet (selectedUnits [i]) && Vector3.Distance(selectedUnits [i].transform.position, hit.transform.position) <= selectedUnits[i].GetComponent<UnitStats> ().range) {
								selectedUnits [i].GetComponent<UnitStats> ().DoDamage(hit.transform.gameObject);
							}
						}

					}
					//if it hits a walkable object but not a player
					if (!(Physics.CheckSphere (hit.point, checkRadius, unwalkableMask)) && !(Physics.CheckSphere (hit.point, checkRadius, playerMask))&& !(Physics.CheckSphere (hit.point, checkRadius, npcMask))) {
						//check that there is a player selected
						if(selectedUnits.Count != 0){
							if (!combatMode) {
								for (int i = 0; i < selectedUnits.Count; i++) {
									//move the players!
									//print("moving outside of combat scenario");
									selectedUnits [i].GetComponent<UnitHandler> ().StartPathGoing (CharacterFindPlace (hit.point, i));
								}
							} else {
							//Combat mode
								for (int i = 0; i < selectedUnits.Count; i++) {
									//move the players!
									if (MyTurnYet (selectedUnits [i])) {
										//tests
										//print("current turn in selected unit @ I is "+MyTurnYet(selectedUnits[i])+" is "+selectedUnits[i].ToString());
										//print("current turn in battlelineup is "+MyTurnYet(battleLineUp[currentTurn])+" is "+ battleLineUp[currentTurn].ToString());
										//
										selectedUnits [i].GetComponent<UnitHandler> ().CombatPathGoing (CharacterFindPlace (hit.point, i));

									}
								}
							
							}

						}
					}
				}
			}
			dragSelect = false;
			lMouseDown = false;
			dClick = false;
		}
																						//THIS CONTROLS COMBAT
		if(combatMode){
			if(!turnInProgress){
				turnInProgress = true;
				if (currentTurn >= battleLineUp.Count) {
					currentTurn = 0;
				}
				cameraController.CentreCamera (battleLineUp [currentTurn].transform.position);
				if(battleLineUp[currentTurn].CompareTag("Player")){
					selectedUnits.Clear ();
					selectedUnits.Add(battleLineUp[currentTurn]);
				}
				StartCoroutine (TakeTurns());
			}
		}



	}

	IEnumerator TakeTurns(){
		battleLineUp [currentTurn].GetComponent<UnitHandler> ().CombatGeneric ();
		yield return new WaitForSeconds(turnDelay);
	
	}
	//take the hit point and array number and place them in a triangular fashion // player controlled characters default shape when moved (party members together)
	Vector3 CharacterFindPlace (Vector3 point, float place)
	{

		// 2X  for now will need to adjust later!
		Vector3 tempVec = new Vector3 (point.x, point.y, point.z - (2 * place));
		return tempVec;


	}
	//drag select checking stuff!
	public bool IsWithinSelectionBounds (GameObject gameobject)
	{
		if (!dragSelect) {
			return false;
		}

		var camera = Camera.main;
		var viewportBounds = Utils.GetViewportBounds (camera, mousePosition1, Input.mousePosition);
		return viewportBounds.Contains (camera.WorldToViewportPoint (gameobject.transform.position));

	}

	// GUI stuff! -> would moving all GUI stuff to another C# script be more efficent?
	void OnGUI ()
	{
		//preview stuff
		if (selectedUnits.Count > 0 && dragSelect == false) {
			haveplayers = true;
			previewCube.SetActive (true);
		} else {
			haveplayers = false;
			previewCube.SetActive (false);
		}
	
		if (haveplayers) {
			//draw a box on NodeFromWorldPoint(mouse raycast pos)
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit previewHit;
			if (Physics.Raycast (ray, out previewHit,Mathf.Infinity,floorMask)) {
				//print ("(pH) you hit = " + previewHit.point);
				previewNode = grid.NodeFromWorldPoint(previewHit.point);
				previewCube.transform.position = new Vector3( previewNode.worldPosition.x,0.25f,previewNode.worldPosition.z);

			}
		}
		//drag select box
		if (dragSelect) {
			var rect = Utils.GetScreenRect (mousePosition1, Input.mousePosition);
			Utils.DrawScreenRect (rect, new Color (0.8f, 0.8f, 0.95f, 0.25f));
			Utils.DrawScreenRectBorder (rect, 2, new Color (0.8f, 0.8f, 0.95f));
		}


	}
	private void SortCombatLineupUp(int currentInt){
		if (currentInt == 0) {
			return;
		}
			//recursively go up the list sorting from the bottom up
			if (battleLineUp[currentInt].GetComponent<UnitStats>().quickness > battleLineUp[currentInt-1].GetComponent<UnitStats>().quickness) {
				GameObject temp = battleLineUp [currentInt];
				battleLineUp [currentInt] = battleLineUp [currentInt - 1];
				battleLineUp [currentInt -1 ] = temp;
				SortCombatLineupUp (currentInt - 1);
			}
		
	}
	private bool CheckEnemy(){
		//returns true when combat finished
		//check if any enemys are in combat mode
		for(int i = 0; i < battleLineUp.Count; i ++){
			if(battleLineUp[i].CompareTag("Enemy")){
				return false;
			}
		}
		return true;
	}

	// global public functions
	public void MyTurnDone(){
		turnInProgress = false;
		currentTurn++;
	}
	public bool MyTurnYet(GameObject me){
		if (me.Equals (battleLineUp [currentTurn])) {
			return true;
		}
		return false;
	}
	public bool GetCombat(){
	
		return combatMode;

	}
	public void ImDead(GameObject deceased){
		battleLineUp.Remove (deceased);
		Destroy (deceased);
		if(CheckEnemy()){
			//end combat mode
			combatMode = false;
		}
	}
	//publlically accessed functions (accessed by the players UnitController
	public void PlayersTurn(){
		playerTurn = true;

	}
	// Publically accessed functions (mostly accessed from enemy)
	public Grid getGrid(){
		return grid;
	}
	public GameObject[] getPlayers(){
		return availableUnits;
	}

	// these two could have been done so much better im so sorry (i should be using get set future me please do this)
	public void switchCombat(bool state){
		if (!combatMode) {
			combatMode = state;
			playerTurn = false;
			for (int i = 0; i < selectedUnits.Count; i++) {
				//move the players!
				selectedUnits [i].GetComponent<UnitHandler> ().StartCombatMode ();
			}
		} else {
			//this is if in the future ill need an Npc to stop combat mode
			combatMode = state;
		}
	}
	public void addcombatant(GameObject fighter){
		battleLineUp.Add (fighter);
		//combatMode = true;
		SortCombatLineupUp(battleLineUp.Count-1);
	}
}		