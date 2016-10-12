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

    /**< 墙纸地板纹理默认类型 */
    string WALL_FLOOR_TEX_TYPE = ".jpg";
    int exportTexNum = 0;

    /**< 窗户TAG类型 */
    string WINDOW_TYPE_TAG = "window";
    int portalLightNum = 0;

    EssWriter essWriter = new EssWriter();

    Matrix4x4 l2rMatrix = new Matrix4x4();

    List<string> renderInstList = new List<string>();

    Dictionary<string, uint> meshMap = new Dictionary<string, uint>(); //meshname - ref count

    //Dictionary<string, uint> texMap = new Dictionary<string, uint>(); //texture name - ref count

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

        portalLightNum = 0;
        exportTexNum = 0;

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

        //addWallFloorTexCoord();

        //addDefaultMtl();

        //GameObject[] dirLights = GameObject.FindGameObjectsWithTag(DIR_LIGHT_TAG);
        //foreach(GameObject lightObj in dirLights)
        //{
        //    Light light = (Light)lightObj.GetComponent("Light");
        //    addDirLight(light);
        //}

        //GameObject[] allObjects = ParentNodeManager.Instance.GetChildObjArray();
        GameObject[] furObjs = GameObject.FindGameObjectsWithTag("furniture");
        GameObject[] windows = GameObject.FindGameObjectsWithTag("window");
        GameObject[] wallObjs = GameObject.FindGameObjectsWithTag("wall");
        GameObject[] allObjects = new GameObject[furObjs.Length + wallObjs.Length + windows.Length];
        furObjs.CopyTo(allObjects, 0);
        wallObjs.CopyTo(allObjects, furObjs.Length);
        int windowStartIndex = furObjs.Length + wallObjs.Length;
        windows.CopyTo(allObjects, windowStartIndex);

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

                    Vector2[] uvs = exportMesh.uv;
                    Vector2 uvScale = getUVScale(gameObj.transform.localScale);
                    string useMtlName = addDefaultMtl(gameObj.GetComponent<Renderer>().material.mainTexture.name, uvScale);
                    addVertexRenderInst(regularMeshName(gameObj.name), objMat.transpose, vertexs, indexs, uvs, useMtlName);                    

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

                    //portal light create from window
                    if(gameObj.tag == WINDOW_TYPE_TAG)
                    {
                        int offsetZ = 30;
                        float portalLightIntensity = 1.0f;
                        gameObj.transform.localScale = Vector3.one / ESS_SCALE_EFFI;
                        gameObj.transform.Rotate(-Vector3.right * ESS_ROTATE_ON_XAXIS_DEGREE);
                        gameObj.transform.Translate(-Vector3.forward * offsetZ);
                        Matrix4x4 portalLightMat = l2rMatrix * gameObj.transform.localToWorldMatrix * l2rMatrix;
                        addPortalLight(200, 200, portalLightMat.transpose, portalLightIntensity);
                    }                    
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
                    Mesh mesh = childTrans.GetComponent<MeshFilter>().mesh;
                    Vector3[] vertexs = mesh.vertices;
                    int[] indexs = mesh.GetIndices(0);

                    Vector2[] uvs = mesh.uv;

                    Matrix4x4 objMat = childTrans.localToWorldMatrix;
                    objMat = l2rMatrix * objMat;

                    Vector2 uvScale = getUVScale(childTrans.localScale);
                    string useMtlName = addDefaultMtl(childTrans.GetComponent<Renderer>().material.mainTexture.name, uvScale);
                    addVertexRenderInst(regularMeshName(childTrans.gameObject.name), objMat.transpose, vertexs, indexs, uvs, useMtlName);
                }

                FindChild( childTrans );
            }

        }
    }

    Vector2 getUVScale(Vector3 scale)
    {
        Vector2 uvScale = new Vector2();
        if(scale.x < 1.0f)
        {
            uvScale.x = scale.y;
            uvScale.y = scale.z;
        }
        else if(scale.y < 1.0)
        {
            uvScale.x = scale.x;
            uvScale.y = scale.z;
        }
        else if(scale.z < 1.0f)
        {
            uvScale.x = scale.x;
            uvScale.y = scale.y;
        }

        return uvScale;
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
        essWriter.AddScaler( "focal", 120.0f );
        essWriter.AddScaler( "aperture", 200.0f );
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
        essWriter.AddInt("diffuse_depth", 5);
        essWriter.AddInt("sum_depth", 10);
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

    void addPortalLight(int width, int height, Matrix4x4 tran, float intensity)
    {
        string portalLightNode = "portal_light_node";

        if(portalLightNum == 0)
        {
            essWriter.BeginNode("portallight", portalLightNode);
            essWriter.AddRef("map", "environment_shader");
            essWriter.AddColor("color", Vector3.one);
            essWriter.AddScaler("width", width);
            essWriter.AddScaler("height", height);
            essWriter.AddScaler("intensity", intensity);
            essWriter.AddInt("samples", 16);
            essWriter.EndNode();
        }        

        string portalLightInst = "portal_light_inst" + portalLightNum;
        essWriter.BeginNode("instance", portalLightInst);
        essWriter.AddRef("element", portalLightNode);
        essWriter.AddBool("visible_primary", false);
        essWriter.AddBool("cast_shadow", false);
        essWriter.AddMatrix("transform", tran);
        essWriter.AddMatrix("motion_transform", tran);
        essWriter.EndNode();

        renderInstList.Add(portalLightInst);

        portalLightNum += 1;
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
        string hdriShaderName = addHDRIEnvMapShader(hdrImageName, 0, 2.0f);
        essWriter.BeginNode("output_result", "global_environment");
        essWriter.LinkParam("input", hdriShaderName, "result");
        essWriter.AddBool("env_emits_GI", false);
        essWriter.EndNode();

        essWriter.BeginNode( "osl_shadergroup", "environment_shader" );
        List<string> refList = new List<string>();
        refList.Add( "global_environment" );
        essWriter.AddRefGroup( "nodes", refList );
        essWriter.EndNode();
    }

    void addWallFloorTexCoord(string uvgenName, Vector2 uvScale)
    {        
        essWriter.BeginNode("max_stduv", uvgenName);
	    essWriter.AddToken("mapChannel", "uv0");
        essWriter.AddScaler("uOffset", 0.0f);
        essWriter.AddScaler("uScale", uvScale.x);
        essWriter.AddScaler("vScale", uvScale.y);
	    essWriter.AddBool("uWrap", true);	    
	    essWriter.AddBool("vWrap", true);
	    essWriter.AddInt("slotType", 1);
	    essWriter.AddInt("coordMapping", 4);
        essWriter.AddInt("uvwSource", 0);
        essWriter.AddBool("hideMapBack", false);
        essWriter.AddBool("uMirror", false);
        essWriter.AddBool("vMirror", false);
        essWriter.AddScaler("uAngle", 0.0f);
        essWriter.AddScaler("vAngle", 0.0f);
        essWriter.AddScaler("wAngle", 0.0f);
        essWriter.AddInt("axis", 0);
        essWriter.AddBool("clip", true);
        essWriter.AddScaler("blur", 1.0f);
        essWriter.AddScaler("blurOffset", 0.0f);
        essWriter.AddBool("uvNoise", false);
        essWriter.AddBool("uvNoiseAnimate", false);
        essWriter.AddScaler("uvNoiseAmount", 1.0f);
        essWriter.AddScaler("uvNoiseSize", 1.0f);
        essWriter.AddInt("uvNoiseLevel", 1);
        essWriter.AddScaler("uvNoisePhase", 0.0f);
        essWriter.AddBool("realWorldScale", false);
	    essWriter.EndNode();
    }

    string addDefaultMtl(string texName, Vector2 uvScale)
    {
        //bitmap         
        string defaultMtlName = "standard_mtl";
        string defaultUvGenName = "wall_floor_uvgen";

        int currIndex = exportTexNum;
        string texNode = texName.Split('.')[0] + "_texnode";

        string uvGenName = defaultUvGenName + currIndex;
        addWallFloorTexCoord(uvGenName, uvScale);

        essWriter.BeginNode("max_bitmap", texNode);
        essWriter.LinkParam("tex_coords", uvGenName, "result");
        essWriter.AddToken("tex_fileName", texName + WALL_FLOOR_TEX_TYPE);
        essWriter.AddInt("tex_alphaSource", 0);
        essWriter.EndNode();

        string stdShadar = "standard_shader" + currIndex;
        essWriter.BeginNode( "max_ei_standard", stdShadar);
        essWriter.AddScaler("specular_weight", 0.0f);
        essWriter.AddScaler("diffuse_weight", 0.9f);
        essWriter.LinkParam("diffuse_color", texNode, "result");
        essWriter.EndNode();

        string backfaceShader = "backface_shader" + currIndex;
        essWriter.BeginNode("backface_cull", backfaceShader);
        essWriter.LinkParam("material", stdShadar, "result");
        essWriter.EndNode();

        string resultStdShader = "result_stand_shader" + currIndex;
        essWriter.BeginNode("max_result", resultStdShader);
        essWriter.LinkParam("input", backfaceShader, "result");
        essWriter.EndNode();

        string stdShaderGroup = "stand_shader_group" + currIndex;
        essWriter.BeginNode("osl_shadergroup", stdShaderGroup);
        List<string> groups = new List<string>();
        groups.Add( resultStdShader );
        essWriter.AddRefGroup( "nodes", groups );
        essWriter.EndNode();

        string useMtlName = defaultMtlName + currIndex;
        essWriter.BeginNode( "material",  useMtlName);
        essWriter.AddRef("surface_shader", stdShaderGroup);
        essWriter.EndNode();

        exportTexNum += 1;

        return useMtlName;
        
    }

    void addEssMeshData(string nodeName, Vector3[] vertexs, int[] indexs, Vector2[] uvs)
    {
        essWriter.BeginNode( "poly", nodeName );
        essWriter.AddPointArray( "pos_list", vertexs );
        essWriter.AddIndexArray( "triangle_list", indexs, false );
        essWriter.AddDeclare("vector[]", "uv0", "varying");
        essWriter.AddUvArray("uv0", uvs);
        essWriter.EndNode();
    }

    void addVertexRenderInst( string meshName, Matrix4x4 transform, Vector3[] vertexs, int[] indexs, Vector2[] uvs , string useMtlName)
    {
        string nodeName = meshName + "_node";
        if( !meshMap.ContainsKey( meshName ) )
        {
            addEssMeshData( nodeName, vertexs, indexs, uvs );
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
        mtlList.Add( useMtlName );
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
        string essInstName = meshName;
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
        string instGroup = essInstName + MESH_SPACE_SUFFIX + "::" + MAX_EXPORT_ESS_DEFAULT_INST_NAME;
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
