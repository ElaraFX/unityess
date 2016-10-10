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
using System.Collections.Generic;
using UnityEngine;

public class ExportESS
{
    /**< MAX导出ESS默认的Inst Group Name */
    string MAX_EXPORT_ESS_DEFAULT_INST_NAME = "mtoer_instgroup_00";

    string MESH_SPACE_SUFFIX = "_space";

    /**< 需要导出的家具TAG类型 */
    public string[] exportFurnitureModelTags = new[] { "floor", "wall" };

    //public string[] mappingFurnitureModelTags = new[] { "furniture" };

    /**< parse ess inst 绕 x 轴旋转角度 */
    int ESS_ROTATE_ON_XAXIS_DEGREE = -90;

    /**< parse ess inst 缩放系数 */
    float ESS_SCALE_EFFI = 0.001f;

    /**< 方向光TAG类型 */
    string DIR_LIGHT_TAG = "dir_light";

    EssWriter essWriter = new EssWriter();

    Matrix4x4 l2rMatrix = new Matrix4x4();

    List<string> renderInstList = new List<string>();

    Dictionary<string, uint> meshMap = new Dictionary<string, uint>(); //meshname - ref count

    private static ExportESS instance = null;

    public static ExportESS Instance
    {
        get
        {
            if( null == instance )
            {
                instance = new ExportESS();
            }

            return instance;
        }
    }

    public ExportESS( string essPath, string essFileName )
    {
        essWriter.Initialize( essPath, essFileName );
    }

    public ExportESS()
    {
        essWriter.Initialize();
    }

    void resetData()
    {
        Vector4 v1 = new Vector4( 1, 0, 0, 0 );
        Vector4 v2 = new Vector4( 0, 1, 0, 0 );
        Vector4 v3 = new Vector4( 0, 0, -1, 0 );
        Vector4 v4 = new Vector4( 0, 0, 0, 1 );
        l2rMatrix.SetColumn( 0, v1 );
        l2rMatrix.SetColumn( 1, v2 );
        l2rMatrix.SetColumn( 2, v3 );
        l2rMatrix.SetColumn( 3, v4 );

        renderInstList.Clear();
        meshMap.Clear();
    }

    /** 导出场景接口
     * \return string ess的字符流数据
     */
    public string ExportFromScene( Camera cam )
    {
        resetData();

        addEssOption();

        addGlobalEnvLight("013.hdr");

        //Camera cam = Camera.main;

        if( null == cam )
        {
            Debug.LogError( "the scene is not exist mainCamera" );

            return null;
        }

        addCameraData( cam );

        addDefaultMtl();

        //GameObject[] dirLights = GameObject.FindGameObjectsWithTag(DIR_LIGHT_TAG);
        //foreach(GameObject lightObj in dirLights)
        //{
        //    Light light = (Light)lightObj.GetComponent("Light");
        //    addDirLight(light);
        //}

        GameObject[] allObjects = ParentNodeManager.Instance.GetChildObjArray();
        //GameObject[] furObjs = GameObject.FindGameObjectsWithTag("furniture");
        //GameObject[] wallObjs = GameObject.FindGameObjectsWithTag("wall");
        //GameObject[] allObjects = new GameObject[furObjs.Length + wallObjs.Length];
        //furObjs.CopyTo(allObjects, 0);
        //wallObjs.CopyTo(allObjects, furObjs.Length);

        foreach(GameObject gameObj in allObjects)
        {
            MeshFilter viewedModelFilter = null;

            viewedModelFilter = gameObj.GetComponent<MeshFilter>();

            if( null == viewedModelFilter )
            {
                viewedModelFilter = gameObj.transform.GetChild( 0 ).GetComponent<MeshFilter>();
            }

            if(viewedModelFilter)
            {
                int finder = Array.IndexOf(exportFurnitureModelTags, gameObj.tag);
                if(finder >= 0)
                {
                    Mesh exportMesh = viewedModelFilter.mesh;
                    Vector3[] vertexs = exportMesh.vertices;
                    int[] indexs = exportMesh.GetIndices(0);
                    
                    Matrix4x4 objMat = gameObj.transform.localToWorldMatrix;
                    objMat = l2rMatrix * objMat;

                    addVertexRenderInst(regularMeshName(gameObj.name), objMat.transpose, vertexs, indexs);

                    FindChild(gameObj.transform);
                }
                else
                {
                    string meshName = gameObj.name;
                    
                    gameObj.transform.localScale = Vector3.one * ESS_SCALE_EFFI;
                    gameObj.transform.Rotate(Vector3.right * ESS_ROTATE_ON_XAXIS_DEGREE);

                    Matrix4x4 objMat = gameObj.transform.localToWorldMatrix;
                    objMat = l2rMatrix * objMat;
                    addMappingInst(regularMeshName(meshName), objMat.transpose);
                }
            }
        }

        addInstanceGroup();
        addRenderCommand();

        essWriter.Close();

        return essWriter.getEssDataString();
    }

    private void FindChild( Transform child )
    {
        if( null != child )
        {
            int length = child.childCount;

            for( int i = 0; i < length; i++ )
            {
                Transform childTrans = child.GetChild( i );

                if( childTrans.CompareTag( "wall" ) )
                {
                    Mesh mesh = childTrans.GetComponent<Mesh>();
                    Vector3[] vertexs = mesh.vertices;
                    int[] indexs = mesh.GetIndices(0);

                    Matrix4x4 objMat = childTrans.localToWorldMatrix;
                    objMat = l2rMatrix * objMat;

                    addVertexRenderInst(regularMeshName(childTrans.gameObject.name), objMat.transpose, vertexs, indexs);
                }

                FindChild( childTrans );
            }

        }
    }

    string regularMeshName(string inMeshName)
    {
        string[] retStrings = inMeshName.Split('&');
        return retStrings[0];
    }

    void addCameraData( Camera cam )
    {
        essWriter.BeginNode( "camera", "cam1" );
        essWriter.AddRef( "env_shader", "environment_shader" );
        essWriter.AddScaler( "aspect", cam.aspect );
        essWriter.AddScaler( "focal", 50.0f );
        essWriter.AddScaler( "aperture", 144.724029f );
        essWriter.AddInt( "res_x", cam.pixelWidth );
        essWriter.AddInt( "res_y", cam.pixelHeight );
        essWriter.EndNode();

        essWriter.BeginNode( "instance", "caminst1" );
        essWriter.AddRef( "element", "cam1" );
        Matrix4x4 camMat = cam.transform.localToWorldMatrix;
        camMat = l2rMatrix * camMat * l2rMatrix;
        essWriter.AddMatrix( "transform", camMat.transpose );
        essWriter.AddMatrix( "motion_transform", camMat.transpose );
        essWriter.EndNode();
    }

    void addEssOption()
    {
        essWriter.BeginNode( "options", "opt" );
        essWriter.AddEnum( "filter", "gaussian" );
        essWriter.AddScaler( "filter_size", 3.0f );
        essWriter.AddScaler( "display_gamma", 2.2f );
        essWriter.EndNode();
    }

    void addDirLight( Light light )
    {
        string sunName = "dir_sun_light";
        essWriter.BeginNode( "directlight", sunName );
        essWriter.AddScaler( "intensity", light.intensity );
        essWriter.AddEnum( "face", "front" );
        essWriter.AddColor( "color", light.color );
        essWriter.EndNode();

	    string instanceName = sunName + "_instance";
	    essWriter.BeginNode("instance", instanceName);
	    essWriter.AddRef("element",sunName);
	    essWriter.AddBool("cast_shadow", false);
	    essWriter.AddBool("shadow", true);
        Matrix4x4 mat = l2rMatrix * light.transform.localToWorldMatrix * l2rMatrix;
	    essWriter.AddMatrix("transform", mat.transpose);
	    essWriter.AddMatrix("motion_transform", mat.transpose);
	    essWriter.EndNode();

        renderInstList.Add(instanceName);
    }

    string addHDRIEnvMapShader(string hdriFileName, float rotation, float intensity)
    {
        string hdriBaseName = "hdri_env";
        string uvGenName = hdriBaseName + "_uvgen";
        essWriter.BeginNode("max_stduv", uvGenName);
	    essWriter.AddToken("mapChannel", "uv0");
        essWriter.AddScaler("uOffset", rotation * Mathf.Deg2Rad);
	    essWriter.AddScaler("uScale", 1.0f);
	    essWriter.AddBool("uWrap", true);
	    essWriter.AddScaler("vScale", 1.0f);
	    essWriter.AddBool("vWrap", true);
	    essWriter.AddInt("slotType", 1);
	    essWriter.AddInt("coordMapping", 1);
	    essWriter.EndNode();

        string bitmapName = hdriBaseName + "_bitmap";
	    essWriter.BeginNode("max_bitmap", bitmapName);
	    essWriter.LinkParam("tex_coords", uvGenName, "result");
        essWriter.AddToken("tex_fileName", hdriFileName);
	    essWriter.AddInt("tex_alphaSource", 0);
	    essWriter.EndNode();

        string stdoutName = hdriBaseName + "_stdout";
	    essWriter.BeginNode("max_stdout", stdoutName);
	    essWriter.AddInt("useColorMap", 0);
	    essWriter.AddScaler("outputAmount", intensity);
	    essWriter.LinkParam("stdout_color", bitmapName, "result");
	    essWriter.EndNode();

	    return stdoutName;
    }

    void addGlobalEnvLight(string hdrImageName)
    {
        string hdriShaderName = addHDRIEnvMapShader(hdrImageName, 0, 1.0f);
        essWriter.BeginNode("output_result", "global_environment");
        essWriter.LinkParam("input", hdriShaderName, "result");
        essWriter.AddBool("env_emits_GI", true);
        essWriter.EndNode();

        essWriter.BeginNode( "osl_shadergroup", "environment_shader" );
        List<string> refList = new List<string>();
        refList.Add( "global_environment" );
        essWriter.AddRefGroup( "nodes", refList );
        essWriter.EndNode();
    }

    void addDefaultMtl()
    {
        essWriter.BeginNode( "max_ei_standard", "standard_shader" );
        essWriter.AddScaler("specular_weight", 0.0f);
        essWriter.AddScaler("diffuse_weight", 0.9f);
        essWriter.EndNode();

        essWriter.BeginNode("backface_cull", "backface_shader");
        essWriter.LinkParam("material", "standard_shader", "result");
        essWriter.EndNode();

        essWriter.BeginNode( "max_result", "result_stand_shader" );
        essWriter.LinkParam("input", "backface_shader", "result");
        essWriter.EndNode();

        essWriter.BeginNode( "osl_shadergroup", "stand_shader_group" );
        List<string> groups = new List<string>();
        groups.Add( "result_stand_shader" );
        essWriter.AddRefGroup( "nodes", groups );
        essWriter.EndNode();

        essWriter.BeginNode( "material", "standard_mtl" );
        essWriter.AddRef( "surface_shader", "stand_shader_group" );
        essWriter.EndNode();
    }

    void addEssMeshData( string nodeName, Vector3[] vertexs, int[] indexs )
    {
        essWriter.BeginNode( "poly", nodeName );
        essWriter.AddPointArray( "pos_list", vertexs );
        essWriter.AddIndexArray( "triangle_list", indexs, false );
        essWriter.EndNode();
    }

    void addVertexRenderInst( string meshName, Matrix4x4 transform, Vector3[] vertexs, int[] indexs )
    {
        string nodeName = meshName + "_node";
        if( !meshMap.ContainsKey( meshName ) )
        {
            addEssMeshData( nodeName, vertexs, indexs );
            meshMap.Add( meshName, 1 );
        }
        else
        {
            meshMap[meshName] += 1;
            meshName += meshMap[meshName];
        }

        essWriter.BeginNode( "instance", meshName );
        essWriter.AddRef( "element", nodeName );
        List<string> mtlList = new List<string>();
        mtlList.Add( "standard_mtl" );
        essWriter.AddRefGroup( "mtl_list", mtlList );
        essWriter.AddMatrix( "transform", transform );
        essWriter.AddMatrix( "motion_transform", transform );
        essWriter.EndNode();

        renderInstList.Add( meshName );
    }

    void addParseMesh(string meshName)
    {
        essWriter.BeginNameSpace(meshName + MESH_SPACE_SUFFIX);
        essWriter.addParseEss(meshName + ".ess");
        essWriter.EndNameSpace();
    }

    void addMappingInst(string meshName, Matrix4x4 tran)
    {
        if (!meshMap.ContainsKey(meshName))
        {
            addParseMesh(meshName);
            meshMap.Add(meshName, 1);
        }
        else
        {
            meshMap[meshName] += 1;
            meshName += meshMap[meshName];
        }

        essWriter.BeginNode("instance", meshName);
        string instGroup = meshName + MESH_SPACE_SUFFIX + "::" + MAX_EXPORT_ESS_DEFAULT_INST_NAME;
        essWriter.AddRef("element", instGroup);
        essWriter.AddMatrix("transform", tran);
        essWriter.AddMatrix("motion_transform", tran);
        essWriter.EndNode();

        renderInstList.Add(meshName);
    }

    void addInstanceGroup()
    {
        essWriter.BeginNode( "instgroup", "world" );
        renderInstList.Add( "caminst1" );
        essWriter.AddRefGroup( "instance_list", renderInstList );
        essWriter.EndNode();
    }

    void addRenderCommand()
    {
        essWriter.AddRenderCommand( "world", "caminst1", "opt" );
    }
}
