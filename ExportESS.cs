/**************************************************************************
 * Copyright (C) 2016 Rendease Co., Ltd.
 * All rights reserved.
 *
 * This program is commercial software: you must not redistribute it 
 * and/or modify it without written permission from Rendease Co., Ltd.
 *
 * This program is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 * End User License Agreement for more details.
 *
 * You should have received a copy of the End User License Agreement along 
 * with this program.  If not, see <http://www.rendease.com/licensing/>
 *************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExportESS : MonoBehaviour {

    /**< 需要导出的家具TAG类型 */
    public string[] exportFurnitureModelTags = new[] { "furniture" };

    EssWriter essWriter = new EssWriter();

    Matrix4x4 l2rMatrix = new Matrix4x4();

    List<string> renderInstList = new List<string>();
    Dictionary<string, uint> meshMap = new Dictionary<string, uint>(); //meshname - ref count

    public ExportESS(string essPath, string essFileName)
    {
        essWriter.Initialize(essPath, essFileName);
    }

    public ExportESS()
    {
        essWriter.Initialize();
    }


	// Use this for initialization
	void Start () {
               
	}

    void resetData()
    {
        Vector4 v1 = new Vector4(1, 0, 0, 0);
        Vector4 v2 = new Vector4(0, 1, 0, 0);
        Vector4 v3 = new Vector4(0, 0, -1, 0);
        Vector4 v4 = new Vector4(0, 0, 0, 1);
        l2rMatrix.SetColumn(0, v1);
        l2rMatrix.SetColumn(1, v2);
        l2rMatrix.SetColumn(2, v3);
        l2rMatrix.SetColumn(3, v4);

        renderInstList.Clear();
        meshMap.Clear();
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    /** 导出场景接口
     * \return string ess的字符流数据
     */
    public string ExportFromScene()
    {
        resetData();

        addEssOption();

        addGlobalEnvLight();

        Camera cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        addCameraData(cam);

        addDefaultMtl();

        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject gameObj in allObjects)
        {
            int findRet = Array.IndexOf(exportFurnitureModelTags, gameObj.tag);
            if(findRet >= 0)
            {
                MeshFilter viewedModelFilter = (MeshFilter)gameObj.GetComponent("MeshFilter");
                if(viewedModelFilter)
                {
                    Mesh exportMesh = viewedModelFilter.mesh;
                    Vector3[] vertexs = exportMesh.vertices;
                    int[] indexs = exportMesh.GetIndices(0);
                         
                    Matrix4x4 objMat = gameObj.transform.localToWorldMatrix;
                    objMat = l2rMatrix * objMat;

                    addRenderInst(gameObj.name, objMat.transpose, vertexs, indexs);                
                }
                        
            }
        }

        addInstanceGroup();
        addRenderCommand();

        essWriter.Close();

        return essWriter.getEssDataString();
    }

    void addCameraData(Camera cam)
    {
        essWriter.BeginNode("camera", "cam1");
        essWriter.AddRef("env_shader", "environment_shader");
        essWriter.AddScaler("aspect", cam.aspect);
        essWriter.AddScaler("focal", 50.0f);
        essWriter.AddScaler("aperture", 144.724029f);
        essWriter.AddInt("res_x", cam.pixelWidth);
        essWriter.AddInt("res_y", cam.pixelHeight);
        essWriter.EndNode();

        essWriter.BeginNode("instance", "caminst1");
        essWriter.AddRef("element", "cam1");
        Matrix4x4 camMat = cam.transform.localToWorldMatrix;
        camMat = l2rMatrix * camMat * l2rMatrix;
        essWriter.AddMatrix("transform", camMat.transpose);
        essWriter.AddMatrix("motion_transform", camMat.transpose);
        essWriter.EndNode();
    }

    void addEssOption()
    {
        essWriter.BeginNode("options", "opt");
        essWriter.AddEnum("filter", "gaussian");
        essWriter.AddScaler("filter_size", 3.0f);
        essWriter.AddScaler("display_gamma", 2.2f);
        essWriter.EndNode();
    }    

    void addDirLight(Light light)
    {
        //暂时不需要导出灯光
        essWriter.BeginNode("directlight", "dir_light");
        essWriter.AddScaler("intensity", light.intensity);
        essWriter.AddEnum("face", "front");
	    essWriter.AddColor("color", light.color);
        essWriter.EndNode();
        
        //TODO:需要添加transform矩阵
    }

    void addGlobalEnvLight()
    {
        essWriter.BeginNode("output_result", "global_environment");
        Vector3 envColor = new Vector3(0.42f, 0.80f, 0.98f);
        essWriter.AddColor("input", envColor);
        essWriter.AddBool("env_emits_GI", true);
        essWriter.EndNode();

        essWriter.BeginNode("osl_shadergroup", "environment_shader");
        List<string> refList = new List<string>();
        refList.Add("global_environment");
        essWriter.AddRefGroup("nodes", refList);
        essWriter.EndNode();
    }

    void addDefaultMtl()
    {
        essWriter.BeginNode("max_ei_standard", "standard_shader");
        essWriter.EndNode();

        essWriter.BeginNode("max_result", "result_stand_shader");
        essWriter.AddCustomString("\tparam_link \"input\" \"standard_shader\" \"result\"");
        essWriter.EndNode();

        essWriter.BeginNode("osl_shadergroup", "stand_shader_group");
        List<string> groups = new List<string>();
        groups.Add("result_stand_shader");
        essWriter.AddRefGroup("nodes", groups);
        essWriter.EndNode();

        essWriter.BeginNode("material", "standard_mtl");
        essWriter.AddRef("surface_shader", "stand_shader_group");
        essWriter.EndNode();
    }

    void addEssMeshData(string nodeName, Vector3[] vertexs, int[] indexs)
    {
        essWriter.BeginNode("poly", nodeName);
        essWriter.AddPointArray("pos_list", vertexs);
        essWriter.AddIndexArray("triangle_list", indexs, false);
        essWriter.EndNode();
    }

    void addRenderInst(string meshName, Matrix4x4 transform, Vector3[] vertexs, int[] indexs)
    {
        string nodeName = meshName + "_node";
        if(!meshMap.ContainsKey(meshName))
        {
            addEssMeshData(nodeName, vertexs, indexs);
            meshMap.Add(meshName, 1);
        }        
        else
        {
            meshMap[meshName] += 1;
            meshName += meshMap[meshName];
        }

        essWriter.BeginNode("instance", meshName);
        essWriter.AddRef("element", nodeName);
        List<string> mtlList = new List<string>();
        mtlList.Add("standard_mtl");
        essWriter.AddRefGroup("mtl_list", mtlList);        
        essWriter.AddMatrix("transform", transform);
        essWriter.AddMatrix("motion_transform", transform);
        essWriter.EndNode();

        renderInstList.Add(meshName);
    }

    void addInstanceGroup()
    {
        essWriter.BeginNode("instgroup", "world");        
        renderInstList.Add("caminst1");
        essWriter.AddRefGroup("instance_list", renderInstList);
        essWriter.EndNode();
    }

    void addRenderCommand()
    {
        essWriter.AddRenderCommand("world", "caminst1", "opt");
    }
}
