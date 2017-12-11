﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
//THIS WAS MADE USING A YOUTUBE TUTORIAL BY SABASTIAN LAGUE- https://www.youtube.com/channel/UCmtyQOKKmrMVaKuRXz02jbQ - specifically the A* Pathfinding tutorial, i do not own this code, however i may have modified it.


public class PathRequestManager : MonoBehaviour {
	
	Queue<PathRequest> pathRequestQueue = new Queue<PathRequest> ();
	PathRequest currentPathRequest;

	static PathRequestManager instance;
	Pathfinding pathfinding;

	bool isProcessingPath;

	void Awake(){

		instance = this;
		pathfinding = GetComponent<Pathfinding> ();
	}

	//request path
	public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[],bool> callback){
		PathRequest newRequest = new PathRequest (pathStart, pathEnd, callback);
		instance.pathRequestQueue.Enqueue (newRequest);
		instance.TryProcessNext ();

	}

	void TryProcessNext(){
		if (!isProcessingPath && pathRequestQueue.Count > 0) {
			currentPathRequest = pathRequestQueue.Dequeue ();
			isProcessingPath = true;
			pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
			
		}
	
	}
	public void FinishedProcessingPath(Vector3[] path, bool success){
	
		currentPathRequest.callback (path, success);
		isProcessingPath = false;
		TryProcessNext ();

	}

	struct PathRequest {

		public Vector3 pathStart;
		public Vector3 pathEnd;
		public Action<Vector3[],bool> callback;

		public PathRequest(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[],bool> callback){
			this.pathStart = pathStart;
			this.pathEnd = pathEnd;
			this.callback = callback;


		}

	}

}
