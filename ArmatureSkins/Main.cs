using BepInEx;
using HarmonyLib;
using Microsoft.Cci;
using SimpleResourceReplacer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using static Xsolla.XsollaPurchase.VirtualItems;
using Object = UnityEngine.Object;

namespace ArmatureSkins
{
    [BepInPlugin("com.aidanamite.ArmatureSkins", "Armature Skins", "1.0.0")]
    [BepInDependency("com.aidanamite.SimpleTextureReplacer")]
    public class Main : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource logger;
        public static readonly string IconPath = "RS_SHARED/customassets/" + DateTime.UtcNow.Ticks + "-Armature Skin";
        public static readonly string IconRunicPath = "RS_SHARED/customassets/" + DateTime.UtcNow.Ticks + "-Runic Armature Skin";
        public static readonly string IconBlenderPath = "RS_SHARED/customassets/" + DateTime.UtcNow.Ticks + "-Blender Armature Skin";
        public static readonly string PrimaryPartPath = "RS_SHARED/ArmatureSkins/" + DateTime.UtcNow.Ticks + "-Primary Part";
        public static readonly string SecondaryPartPath = "RS_SHARED/ArmatureSkins/" + DateTime.UtcNow.Ticks + "-Secondary Part";
        public static readonly string TertiaryPartPath = "RS_SHARED/ArmatureSkins/" + DateTime.UtcNow.Ticks + "-Tertiary Part";
        public static readonly string PrimarySecondaryPartPath = "RS_SHARED/ArmatureSkins/" + DateTime.UtcNow.Ticks + "-Primary-Secondary Part";
        public static readonly string EtchNormalPath = "RS_SHARED/ArmatureSkins/" + DateTime.UtcNow.Ticks + "-Etch Normal";
        public static readonly string EtchMaskPath = "RS_SHARED/ArmatureSkins/" + DateTime.UtcNow.Ticks + "-Etch Mask";
        public const string TaskName = "GenerateArmatureSkins";
        // Original model: "Heavy Metal Squiggle Orb" (https://skfb.ly/ou6S8) by Aiekick is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).
        // Model has been backed as a normal map onto a icosphere
        public static Mesh MetalOrbMesh;
        public static Mesh MonkeyMesh;
        public void Awake()
        {
            logger = Logger;
            var t = new Texture2D(0,0,TextureFormat.RGBA32,false);
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArmatureSkins.icon.png"))
            {
                var b = new byte[s.Length];
                s.Read(b, 0, b.Length);
                t.LoadImage(b, false);
            }
            SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(new ResouceKey(IconPath)).AddAsset(t,0);
            t = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArmatureSkins.icon_runic.png"))
            {
                var b = new byte[s.Length];
                s.Read(b, 0, b.Length);
                t.LoadImage(b, false);
            }
            SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(new ResouceKey(IconRunicPath)).AddAsset(t, 0);
            t = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArmatureSkins.icon_blender.png"))
            {
                var b = new byte[s.Length];
                s.Read(b, 0, b.Length);
                t.LoadImage(b, false);
            }
            SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(new ResouceKey(IconBlenderPath)).AddAsset(t, 0);
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, Color.black);
            t.Apply(false, false);
            SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(new ResouceKey(PrimaryPartPath)).AddAsset(t, 0);
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, Color.green);
            t.Apply(false, false);
            SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(new ResouceKey(SecondaryPartPath)).AddAsset(t, 0);
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, Color.blue);
            t.Apply(false, false);
            SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(new ResouceKey(TertiaryPartPath)).AddAsset(t, 0);
            t = new Texture2D(1, 2, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, Color.black);
            t.SetPixel(0, 1, Color.green);
            t.Apply(false, false);
            t.filterMode = FilterMode.Point;
            SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(new ResouceKey(PrimarySecondaryPartPath)).AddAsset(t, 0);
            t = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArmatureSkins.etch_mask.png"))
            {
                var b = new byte[s.Length];
                s.Read(b, 0, b.Length);
                t.LoadImage(b, false);
            }
            SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(new ResouceKey(EtchMaskPath)).AddAsset(t, 2);
            t = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArmatureSkins.etch_normal.png"))
            {
                var b = new byte[s.Length];
                s.Read(b, 0, b.Length);
                t.LoadImage(b, false);
            }
            SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(new ResouceKey(EtchNormalPath)).AddAsset(t, 2);
            var bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("ArmatureSkins.models.bundle"));
            MetalOrbMesh = bundle.LoadAsset<Mesh>("etchsphere");
            MonkeyMesh = bundle.LoadAsset<Mesh>("monkeyhead");
            bundle.Unload(false);
            new Harmony("com.aidanamite.ArmatureSkins").PatchAll();
            Logger.LogInfo("Loaded");
        }

        public static SkinnedMeshRenderer[] GetDragonBody(GameObject obj)
        {
            var l = new List<SkinnedMeshRenderer>();
            foreach (var s in obj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (s.name != "Saddle" && s.name != "RotationPivot")
                    l.Add(s);
            return l.ToArray();
        }

        public static void Swap<T>(ref T a,ref T b)
        {
            var c = a;
            a = b;
            b = c;
        }
        public static void GenerateSkins(SanctuaryPetTypeInfo[] typeInfo, Action callback)
        {
            var loaded = new Dictionary<string, GameObject>();
            var holder = new GameObject("holder");
            holder.SetActive(false);
            DontDestroyOnLoad(holder);
            void DoGenerate()
            {
                logger.LogInfo("Generating Skins");
                RsResourceManager.UpdateLoadProgress(TaskName, 1);
                var meshes = new Dictionary<string, string[]>();
                foreach (var p in loaded)
                {
                    var body = GetDragonBody(p.Value);
                    if (body.Length == 0)
                    {
                        logger.LogWarning($"Dragon body render was not found for prefab \"{p.Key}\"");
                        continue;
                    }
                    var root = body[0].rootBone;
                    var bones = body[0].bones;
                    var boneToIndex = new Dictionary<Transform, int>();
                    var poses = new Matrix4x4[bones.Length];
                    for (int i = 0; i < bones.Length; i++)
                    {
                        boneToIndex[bones[i]] = i;
                        poses[i] = bones[i].worldToLocalMatrix * root.localToWorldMatrix;
                    }
                    if (!boneToIndex.TryGetValue(root, out _))
                    {
                        logger.LogWarning($"Dragon \"{p.Key}\" bone list does not contain the root bone. This should never happen");
                        continue;
                    }
                    var size = 0f;
                    for (int i = 0; i < bones.Length; i++)
                    {
                        if (bones[i] == root)
                            continue;
                        var bp = bones[i].parent;
                        while (!boneToIndex.TryGetValue(bp, out _))
                            bp = bp.parent;
                        size += (root.InverseTransformPoint(bp.position) - root.InverseTransformPoint(bones[i].position)).magnitude;
                    }
                    size /= bones.Length - 1;
                    size /= 10;
                    var v = new List<Vector3>();
                    var t = new List<int>();
                    var t2 = new List<int>();
                    var b = new List<BoneWeight>();
                    var n = new List<Vector3>();
                    var c = 6;
                    for (int i = 0; i < bones.Length; i++)
                    {
                        var l = v.Count;
                        var bp = root.InverseTransformPoint(bones[i].position);
                        for (int j = 0; j < 12 + c;j++)
                        {
                            var nv = bp;
                            nv[j % 3] += size * (j < c ? 1 : 0.5f) * ((j % 6 < 3) ? 1 : -1);
                            v.Add(nv);
                            nv = new Vector3();
                            nv[j % 3] = ((j % 6 < 3) ? 1 : -1);
                            n.Add(nv);
                            b.Add(new BoneWeight()
                            {
                                boneIndex0 = i,
                                weight0 = 1
                            });
                        }
                        t.AddRange(new[]
                        {
                            l, l + 1, l + 2,
                            l, l + 5, l + 1,
                            l, l + 4, l + 5,
                            l, l + 2, l + 4,
                            l + 3, l + 5, l + 4,
                            l + 3, l + 4, l + 2,
                            l + 3, l + 2, l + 1,
                            l + 3, l + 1, l + 5
                        });
                        if (bones[i] != root)
                        {
                            var bo = bones[i].parent;
                            var ind = 0;
                            while (bo && !boneToIndex.TryGetValue(bo, out ind))
                                bo = bo.parent;
                            if (!bo)
                                continue;
                            ind *= 12 + c;
                            for (int j = 0; j < 3; j++)
                                for (int h = 0; h <= 1; h++)
                                    for (int o = 0; o <= 1; o++)
                                    {
                                        var k = (j + 1) % 3 + o * 3;
                                        if (h == 1)
                                        {
                                            k = (k + 3) % 6;
                                            j = (j + 3) % 6;
                                        }
                                        if (o == 1)
                                            Swap(ref j, ref k);
                                        t2.AddSquare(l + c + j, l + c + k, ind + c + k, ind + c + j);
                                        t2.AddSquare(l + c + k + 6, l + c + j + 6, ind + c + j + 6, ind + c + k + 6);
                                        if (o == 1)
                                            Swap(ref j, ref k);
                                        if (h == 1)
                                            j = (j + 3) % 6;
                                    }
                        }
                    }

                    var mesh = new Mesh();
                    mesh.vertices = v.ToArray();
                    mesh.triangles = t.Concat(t2).ToArray();
                    mesh.boneWeights = b.ToArray();
                    mesh.normals = n.ToArray();
                    mesh.uv = new Vector2[v.Count];
                    mesh.bindposes = poses;
                    mesh.subMeshCount = 2;
                    mesh.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, t.Count));
                    mesh.SetSubMesh(1, new UnityEngine.Rendering.SubMeshDescriptor(t.Count,t2.Count));
                    mesh.RecalculateBounds();
                    mesh.RecalculateTangents();
                    var path = new ResouceKey("ArmatureSkins." + p.Key);
                    meshes[p.Key] = new string[3];
                    meshes[p.Key][0] = path.FullResourceString;
                    SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(path).AddAsset(mesh, 0);

                    v = new List<Vector3>();
                    t = new List<int>();
                    t2 = new List<int>();
                    b = new List<BoneWeight>();
                    var u = new List<Vector2>();
                    n = new List<Vector3>();
                    c = MetalOrbMesh.vertices.Length;

                    for (int i = 0; i < bones.Length; i++)
                    {
                        var l = v.Count;
                        var bp = root.InverseTransformPoint(bones[i].position);
                        v.AddRange(MetalOrbMesh.vertices.Select(x => x * size * 1.5f + bp));
                        u.AddRange(MetalOrbMesh.uv);
                        n.AddRange(MetalOrbMesh.normals);
                        for (int j = 0; j < 12 + c; j++)
                        {
                            if (j < 12)
                            {
                                var nv = bp;
                                nv[j % 3] += size * 0.5f * ((j % 6 < 3) ? 1 : -1);
                                v.Add(nv);
                                u.Add(default);
                                nv = new Vector3();
                                nv[j % 3] = (j % 6 < 3) ? 1 : -1;
                                n.Add(nv);
                            }
                            b.Add(new BoneWeight()
                            {
                                boneIndex0 = i,
                                weight0 = 1
                            });
                        }
                        t.AddRange(MetalOrbMesh.triangles.Select(x => x + l));
                        if (bones[i] != root)
                        {
                            var bo = bones[i].parent;
                            var ind = 0;
                            while (bo && !boneToIndex.TryGetValue(bo, out ind))
                                bo = bo.parent;
                            if (!bo)
                                continue;
                            ind *= 12 + c;
                            for (int j = 0; j < 3; j++)
                                for (int h = 0; h <= 1; h++)
                                    for (int o = 0; o <= 1; o++)
                                    {
                                        var k = (j + 1) % 3 + o * 3;
                                        if (h == 1)
                                        {
                                            k = (k + 3) % 6;
                                            j = (j + 3) % 6;
                                        }
                                        if (o == 1)
                                            Swap(ref j, ref k);
                                        t2.AddSquare(l + c + j, l + c + k, ind + c + k, ind + c + j);
                                        t2.AddSquare(l + c + k + 6, l + c + j + 6, ind + c + j + 6, ind + c + k + 6);
                                        if (o == 1)
                                            Swap(ref j, ref k);
                                        if (h == 1)
                                            j = (j + 3) % 6;
                                    }
                        }
                    }

                    mesh = new Mesh();
                    mesh.vertices = v.ToArray();
                    mesh.triangles = t.Concat(t2).ToArray();
                    mesh.boneWeights = b.ToArray();
                    mesh.uv = u.ToArray();
                    mesh.normals = n.ToArray();
                    mesh.bindposes = poses;
                    mesh.subMeshCount = 2;
                    mesh.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, t.Count));
                    mesh.SetSubMesh(1, new UnityEngine.Rendering.SubMeshDescriptor(t.Count, t2.Count));
                    mesh.RecalculateBounds();
                    mesh.RecalculateTangents();
                    path = new ResouceKey("RunicArmatureSkins." + p.Key);
                    meshes[p.Key][1] = path.FullResourceString;
                    SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(path).AddAsset(mesh, 0);


                    v = new List<Vector3>();
                    t = new List<int>();
                    t2 = new List<int>();
                    b = new List<BoneWeight>();
                    n = new List<Vector3>();
                    u = new List<Vector2>();
                    var ni = new int[bones.Length];
                    var niPos = 0;
                    for (int i = 0; i < bones.Length; i++)
                    {
                        var bo = bones[i];
                        var ind = 0;
                        var len = Vector3.zero;
                        if (bones[i] != root)
                        {
                            bo = bo.parent;
                            while (bo && !boneToIndex.TryGetValue(bo, out ind))
                                bo = bo.parent;
                            if (!bo)
                                continue;
                            len = bones[i].InverseTransformPoint(bo.position);
                        }
                        niPos += (bones[i].name.Contains("Head") ? MonkeyMesh : MetalOrbMesh).vertices.Length;
                        ni[i] = niPos;
                        niPos += len.sqrMagnitude == 0 ? 1 : 5;
                    }
                    for (int i = 0; i < bones.Length; i++)
                    {
                        var nodeMesh = bones[i].name.Contains("Head") ? MonkeyMesh : MetalOrbMesh;
                        c = nodeMesh.vertices.Length;
                        var l = v.Count;
                        var bp = root.InverseTransformPoint(bones[i].position);
                        if (nodeMesh == MonkeyMesh)
                            v.AddRange(nodeMesh.vertices.Select(x => Quaternion.Inverse(root.rotation) * x * size * 5f + bp));
                        else
                            v.AddRange(nodeMesh.vertices.Select(x => x * size * 1.5f + bp));
                        u.AddRange(nodeMesh.uv);
                        if (nodeMesh == MonkeyMesh)
                            n.AddRange(nodeMesh.normals.Select(x => Quaternion.Inverse(root.rotation) * x));
                        else
                            n.AddRange(nodeMesh.normals);
                        t.AddRange(nodeMesh.triangles.Select(x => x + l));
                        v.Add(bp);
                        u.Add(default);
                        n.Add(Vector3.up);
                        var bo = bones[i];
                        var ind = 0;
                        var len = Vector3.zero;
                        if (bones[i] != root)
                        {
                            bo = bo.parent;
                            while (bo && !boneToIndex.TryGetValue(bo, out ind))
                                bo = bo.parent;
                            if (!bo)
                                continue;
                            len = bones[i].InverseTransformPoint(bo.position);
                        }
                        for (int j = 0; j < (len.sqrMagnitude == 0 ? 1 : 5) + c; j++)
                        {
                            if (len.sqrMagnitude != 0 && j < 4)
                            {
                                var nv = new Vector3(0,0, len.magnitude * 0.85f);
                                nv[j % 2] += len.magnitude * ((j < 2) ? 0.1f : -0.1f);
                                nv = root.InverseTransformPoint(bones[i].TransformPoint(Quaternion.LookRotation(len.normalized) * nv));
                                v.Add(nv);
                                u.Add(default);
                                nv = nv - root.InverseTransformPoint(bones[i].TransformPoint(Quaternion.LookRotation(len.normalized) * new Vector3(0, 0, len.magnitude * 0.85f)));
                                n.Add(nv.normalized);
                            }
                            b.Add(new BoneWeight()
                            {
                                boneIndex0 = j < c + 1 ? i : ind,
                                weight0 = 1
                            });
                        }
                        if (len.sqrMagnitude != 0)
                        {
                            t2.AddRange(new[]
                            {
                                l + c + 1, l + c, l + c + 2,
                                l + c + 1, l + c + 4, l + c,
                                l + c + 1, ni[ind], l + c + 4,
                                l + c + 1, l + c + 2, ni[ind],
                                l + c + 3, l + c + 4, ni[ind],
                                l + c + 3, ni[ind], l + c + 2,
                                l + c + 3, l + c + 2, l + c,
                                l + c + 3, l + c, l + c + 4
                            });
                        }
                    }

                    mesh = new Mesh();
                    mesh.vertices = v.ToArray();
                    mesh.triangles = t.Concat(t2).ToArray();
                    mesh.boneWeights = b.ToArray();
                    mesh.uv = u.ToArray();
                    mesh.normals = n.ToArray();
                    mesh.bindposes = poses;
                    mesh.subMeshCount = 2;
                    mesh.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, t.Count));
                    mesh.SetSubMesh(1, new UnityEngine.Rendering.SubMeshDescriptor(t.Count, t2.Count));
                    mesh.RecalculateBounds();
                    mesh.RecalculateTangents();
                    path = new ResouceKey("BlenderArmatureSkins." + p.Key);
                    meshes[p.Key][2] = path.FullResourceString;
                    SimpleResourceReplacer.Main.SingleAssets.GetOrCreate(path).AddAsset(mesh, 0);
                }
                var matShared = new[]
                    {
                        new MaterialProperty{
                            Target = "Baby",
                            Property = "_Glossiness",
                            Value = "0"
                        },
                        new MaterialProperty{
                            Target = "Adult",
                            Property = "_Glossiness",
                            Value = "0"
                        },
                        new MaterialProperty{
                            Target = "Titan",
                            Property = "_Glossiness",
                            Value = "0"
                        },
                        new MaterialProperty{
                            Target = "Baby",
                            Property = "_SpecColor",
                            Value = "000000"
                        },
                        new MaterialProperty{
                            Target = "Adult",
                            Property = "_SpecColor",
                            Value = "000000"
                        },
                        new MaterialProperty{
                            Target = "Titan",
                            Property = "_SpecColor",
                            Value = "000000"
                        }
                    };
                var mat = matShared.Concat(new[]
                    {
                        new MaterialProperty{
                            Target = "BabyBody",
                            Property = "_ColorMask",
                            Value = PrimaryPartPath
                        },
                        new MaterialProperty{
                            Target = "AdultBody",
                            Property = "_ColorMask",
                            Value = PrimaryPartPath
                        },
                        new MaterialProperty{
                            Target = "TitanBody",
                            Property = "_ColorMask",
                            Value = PrimaryPartPath
                        },
                        new MaterialProperty{
                            Target = "BabyEyes",
                            Property = "_ColorMask",
                            Value = SecondaryPartPath
                        },
                        new MaterialProperty{
                            Target = "AdultEyes",
                            Property = "_ColorMask",
                            Value = SecondaryPartPath
                        },
                        new MaterialProperty{
                            Target = "TitanEyes",
                            Property = "_ColorMask",
                            Value = SecondaryPartPath
                        }
                    }).ToArray();
                var mat2 = matShared.Concat(new[]
                    {
                        new MaterialProperty{
                            Target = "BabyBody",
                            Property = "_ColorMask",
                            Value = EtchMaskPath
                        },
                        new MaterialProperty{
                            Target = "AdultBody",
                            Property = "_ColorMask",
                            Value = EtchMaskPath
                        },
                        new MaterialProperty{
                            Target = "TitanBody",
                            Property = "_ColorMask",
                            Value = EtchMaskPath
                        },
                        new MaterialProperty{
                            Target = "BabyBody",
                            Property = "_BumpMap",
                            Value = EtchNormalPath
                        },
                        new MaterialProperty{
                            Target = "AdultBody",
                            Property = "_BumpMap",
                            Value = EtchNormalPath
                        },
                        new MaterialProperty{
                            Target = "TitanBody",
                            Property = "_BumpMap",
                            Value = EtchNormalPath
                        },
                        new MaterialProperty{
                            Target = "BabyEyes",
                            Property = "_ColorMask",
                            Value = TertiaryPartPath
                        },
                        new MaterialProperty{
                            Target = "AdultEyes",
                            Property = "_ColorMask",
                            Value = TertiaryPartPath
                        },
                        new MaterialProperty{
                            Target = "TitanEyes",
                            Property = "_ColorMask",
                            Value = TertiaryPartPath
                        }
                    }).ToArray();
                var mat3 = matShared.Concat(new[]
                    {
                        new MaterialProperty{
                            Target = "BabyBody",
                            Property = "_ColorMask",
                            Value = PrimarySecondaryPartPath
                        },
                        new MaterialProperty{
                            Target = "AdultBody",
                            Property = "_ColorMask",
                            Value = PrimarySecondaryPartPath
                        },
                        new MaterialProperty{
                            Target = "TitanBody",
                            Property = "_ColorMask",
                            Value = PrimarySecondaryPartPath
                        },
                        new MaterialProperty{
                            Target = "BabyEyes",
                            Property = "_ColorMask",
                            Value = TertiaryPartPath
                        },
                        new MaterialProperty{
                            Target = "AdultEyes",
                            Property = "_ColorMask",
                            Value = TertiaryPartPath
                        },
                        new MaterialProperty{
                            Target = "TitanEyes",
                            Property = "_ColorMask",
                            Value = TertiaryPartPath
                        }
                    }).ToArray();
                foreach (var type in typeInfo)
                {
                    var renderers = new HashSet<string>();

                    foreach (var age in type._AgeData)
                        if (age?._PetResList?.Length > 0 && age._PetResList[0]?._Prefab != null && loaded.TryGetValue(age._PetResList[0]._Prefab, out var prefab))
                            foreach (var body in GetDragonBody(prefab))
                                renderers.Add(body.name);
                    string GetMeshPath(RaisedPetStage stage, int index) {
                        if (type._AgeData.Length <= 0 || type._AgeData.Length <= RaisedPetData.GetAgeIndex(stage))
                            return null;
                        var age = type._AgeData[RaisedPetData.GetAgeIndex(stage)];
                        if (age?._PetResList?.Length > 0 && age._PetResList[0]?._Prefab != null && meshes.TryGetValue(age._PetResList[0]._Prefab, out var mesh))
                            return mesh[index];
                        return null;
                    }
                    var skin = new CustomSkin()
                    {
                        ItemID = 2200000 - type._TypeID,
                        HWMaterialData = Array.Empty<MaterialProperty>(),
                        MaterialData = mat,
                        Mesh = new MeshOverrides
                        {
                            Baby = GetMeshPath(RaisedPetStage.BABY,0),
                            Teen = GetMeshPath(RaisedPetStage.TEEN,0),
                            Adult = GetMeshPath(RaisedPetStage.ADULT,0),
                            Titan = GetMeshPath(RaisedPetStage.TITAN,0)
                        },
                        Name = type._NameText.GetLocalizedString() + " Armature",
                        PetType = type._TypeID,
                        RequiredAge = RaisedPetStage.BABY.ToString(),
                        SkinIcon = IconPath,
                        TargetRenderers = renderers.ToArray()
                    };
                    var file = "ArmatureSkins://" + type._Name + "-armature-" + DateTime.UtcNow.Ticks;
                    SimpleResourceReplacer.Main.equipmentFiles[file] = skin;
                    try
                    {
                        skin.Init();
                    }
                    catch (Exception e)
                    {
                        try { skin.Destroy(); } catch { }
                        SimpleResourceReplacer.Main.equipmentFiles.Remove(file);
                        logger.LogError(e);
                    }
                    skin = new CustomSkin()
                    {
                        ItemID = 2200000 - 1000 - type._TypeID,
                        HWMaterialData = Array.Empty<MaterialProperty>(),
                        MaterialData = mat2,
                        Mesh = new MeshOverrides
                        {
                            Baby = GetMeshPath(RaisedPetStage.BABY, 1),
                            Teen = GetMeshPath(RaisedPetStage.TEEN, 1),
                            Adult = GetMeshPath(RaisedPetStage.ADULT, 1),
                            Titan = GetMeshPath(RaisedPetStage.TITAN, 1)
                        },
                        Name = type._NameText.GetLocalizedString() + " Runic Armature",
                        PetType = type._TypeID,
                        RequiredAge = RaisedPetStage.BABY.ToString(),
                        SkinIcon = IconRunicPath,
                        TargetRenderers = renderers.ToArray()
                    };
                    file = "ArmatureSkins://" + type._Name + "-runic-" + DateTime.UtcNow.Ticks;
                    SimpleResourceReplacer.Main.equipmentFiles[file] = skin;
                    try
                    {
                        skin.Init();
                    }
                    catch (Exception e)
                    {
                        try { skin.Destroy(); } catch { }
                        SimpleResourceReplacer.Main.equipmentFiles.Remove(file);
                        logger.LogError(e);
                    }
                    skin = new CustomSkin()
                    {
                        ItemID = 2200000 - 2000 - type._TypeID,
                        HWMaterialData = Array.Empty<MaterialProperty>(),
                        MaterialData = mat3,
                        Mesh = new MeshOverrides
                        {
                            Baby = GetMeshPath(RaisedPetStage.BABY, 2),
                            Teen = GetMeshPath(RaisedPetStage.TEEN, 2),
                            Adult = GetMeshPath(RaisedPetStage.ADULT, 2),
                            Titan = GetMeshPath(RaisedPetStage.TITAN, 2)
                        },
                        Name = type._NameText.GetLocalizedString() + " Blender Armature",
                        PetType = type._TypeID,
                        RequiredAge = RaisedPetStage.BABY.ToString(),
                        SkinIcon = IconBlenderPath,
                        TargetRenderers = renderers.ToArray()
                    };
                    file = "ArmatureSkins://" + type._Name + "-blender-" + DateTime.UtcNow.Ticks;
                    SimpleResourceReplacer.Main.equipmentFiles[file] = skin;
                    try
                    {
                        skin.Init();
                    }
                    catch (Exception e)
                    {
                        try { skin.Destroy(); } catch { }
                        SimpleResourceReplacer.Main.equipmentFiles.Remove(file);
                        logger.LogError(e);
                    }
                }
                Destroy(holder);
                callback?.Invoke();
            }
            int loading = 1;
            var total = 0;
            var setup = false;
            foreach (var type in typeInfo)
            {
                if (type._TypeID <= 0 || type._TypeID > 1000)
                {
                    logger.LogWarning($"Pet type could not be generated due to invalid type id [NameText={type._NameText?.GetLocalizedString()},Name={type._Name},Id={type._TypeID}]");
                    continue;
                }
                var requested = new HashSet<string>();
                foreach (var age in type._AgeData)
                    if (age != null && age._PetResList?.Length > 0 && age._PetResList[0]._Prefab != null && requested.Add(age._PetResList[0]._Prefab.ToLowerInvariant()))
                    {
                        loading++;
                        total++;
                        RsResourceManager.LoadAssetFromBundle(age._PetResList[0]._Prefab, (a,b,c,d,e) => {
                            if (b == RsResourceLoadEvent.COMPLETE)
                            {
                                if (d as GameObject)
                                    loaded[age._PetResList[0]._Prefab] = Instantiate( d as GameObject,holder.transform);
                            }
                            else if (b != RsResourceLoadEvent.ERROR)
                                return;
                            loading--;
                            if (setup)
                                RsResourceManager.UpdateLoadProgress(TaskName, 1 - loading / total);
                            if (loading == 0)
                                DoGenerate();
                            
                        }, typeof(GameObject));
                    }
            }
            setup = true;
            loading--;
            RsResourceManager.UpdateLoadProgress(TaskName, 1 - loading / total);
            if (loading == 0)
                DoGenerate();
        }
    }

    public static class ExtentionMethods
    {
        public static void AddSquare(this IList<int> list, int a, int b, int c, int d)
        {
            list.Add(a);
            list.Add(b);
            list.Add(d);
            list.Add(d);
            list.Add(b);
            list.Add(c);
        }
    }

    [HarmonyPatch(typeof(RsResourceManager), "LoadAssetFromBundle", typeof(string), typeof(string), typeof(RsResourceEventHandler), typeof(Type), typeof(bool), typeof(object))]
    static class Patch_SetupSanctuaryData
    {
        static bool firstrun;
        static void Prefix(string inBundleURL, string inAssetName, ref RsResourceEventHandler inCallback, Type inType, bool inDontDestroy, object inUserData)
        {
            if (!firstrun && inBundleURL == "RS_DATA/PfSanctuaryDataDO.unity3d" && inAssetName == "PfSanctuaryDataDO")
            {
                RsResourceManager.AddLoadProgressTask(Main.TaskName);
                var original = inCallback;
                inCallback = (a, b, c, d, e) =>
                {
                    if (b == RsResourceLoadEvent.COMPLETE && (d is GameObject g ? g.GetComponent<SanctuaryData>() : d) is SanctuaryData s && s && (firstrun = true))
                        Main.GenerateSkins(s._PetTypes, () => original(a, b, c, d, e));
                    else
                        original(a, b, c, d, e);
                };
            }
        }
    }
}