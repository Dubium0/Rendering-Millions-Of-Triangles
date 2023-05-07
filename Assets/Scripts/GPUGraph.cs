using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    public enum TransitionMode {Cycle, Random}

	[SerializeField]
	TransitionMode transitionMode;

	const int MaxResolution = 1000;
	[SerializeField, Range(10, MaxResolution)]
	int resolution = 10;

	

	[SerializeField]
	FunctionLibrary.FunctionName function;

	[SerializeField,Min(0f)]
	float functionDuration =1f, transitionDuration =1f;

	bool transitioning;

	FunctionLibrary.FunctionName transitionFunction;


	float duration;


    ComputeBuffer positionBuffer;

    [SerializeField]
    ComputeShader computeShader;


    [SerializeField]
    Material material;

    [SerializeField]
    Mesh mesh;

    static readonly int 
        positionId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        timeId = Shader.PropertyToID("_Time"),
		transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    private void OnEnable() {
        positionBuffer = new ComputeBuffer(MaxResolution* MaxResolution,3*4);
    }

    private void OnDisable() {
        positionBuffer.Release();
        positionBuffer = null;
    }

	
	void Update () {
		duration += Time.deltaTime;

		if(transitioning){

			if(duration >= transitionDuration){
				duration -=transitionDuration;
				transitioning  = false;
			}
		}
		else if(duration >= functionDuration){
			duration -=functionDuration;
			transitioning  = true;
			transitionFunction  = function;
			PickNextFunction();
		}

        UpdateFunctionOnGPU();

		

		
		
	}

	void PickNextFunction(){

		if(transitionMode == TransitionMode.Cycle){

			function = FunctionLibrary.GetNextFunctionName(function);

		}else{
			function = FunctionLibrary.GetRandomFunctionName(function);
		}
		
	}

    void UpdateFunctionOnGPU(){
        float step = 2f / resolution;

        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId,Time.time);

		if(transitioning){
			computeShader.SetFloat(transitionProgressId,Mathf.SmoothStep(0f,1f,duration/transitionDuration));
		}

		var kernelIndex = (int)function + (int)(transitioning ? transitionFunction: function) *FunctionLibrary.FunctionCount;
        computeShader.SetBuffer(kernelIndex, positionId, positionBuffer);

		

        int groups = Mathf.CeilToInt(resolution / 8f);

        computeShader.Dispatch(kernelIndex, groups,groups,1);

		material.SetBuffer(positionId,positionBuffer);
		material.SetFloat(stepId,step);
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + step));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material,bounds,resolution * resolution);
    }
	


	

	
}
