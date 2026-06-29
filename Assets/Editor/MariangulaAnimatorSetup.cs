using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public class MariangulaAnimatorSetup
{
    [MenuItem("Relatos/Autoconfigurar Mariangula (Animaciones)")]
    public static void SetupAnimator()
    {
        string spriteFolder = "Assets/Sprites/Enemies/Mariangula";
        string animFolder = spriteFolder + "/Animations";

        if (!AssetDatabase.IsValidFolder(animFolder))
        {
            AssetDatabase.CreateFolder(spriteFolder, "Animations");
        }

        // Load Sprites
        Object[] idleObjects = AssetDatabase.LoadAllAssetsAtPath(spriteFolder + "/mari-idle.png");
        Sprite[] idleSprites = idleObjects.OfType<Sprite>().OrderBy(s => s.name).ToArray();

        Object[] runObjects = AssetDatabase.LoadAllAssetsAtPath(spriteFolder + "/mari-run.png");
        Sprite[] runSprites = runObjects.OfType<Sprite>().OrderBy(s => s.name).ToArray();

        if (idleSprites.Length == 0 || runSprites.Length == 0)
        {
            Debug.LogError("No se encontraron los sprites mari-idle.png o mari-run.png en " + spriteFolder + ". Por favor, asegúrate de que el Sprite Mode esté en Multiple.");
            return;
        }

        // Create Clips
        AnimationClip idleClip = CreateClip(idleSprites, 12f, false); // No hace loop, se queda en el último frame
        AnimationClip runClip = CreateClip(runSprites, 12f, true);

        // Borramos los viejos para evitar corrupción en Unity al sobrescribir
        AssetDatabase.DeleteAsset(animFolder + "/Mariangula_Idle.anim");
        AssetDatabase.DeleteAsset(animFolder + "/Mariangula_Run.anim");
        AssetDatabase.DeleteAsset(animFolder + "/MariangulaController.controller");

        AssetDatabase.CreateAsset(idleClip, animFolder + "/Mariangula_Idle.anim");
        AssetDatabase.CreateAsset(runClip, animFolder + "/Mariangula_Run.anim");

        // Create Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(animFolder + "/MariangulaController.controller");
        controller.AddParameter("state", AnimatorControllerParameterType.Int);

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        AnimatorState idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState runState = rootStateMachine.AddState("Run");
        runState.motion = runClip;

        rootStateMachine.defaultState = idleState;

        // Transitions using exactly Jose_Greybox logic (state == 0 and state == 2)
        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0;
        idleToRun.AddCondition(AnimatorConditionMode.Equals, 2, "state");

        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0;
        runToIdle.AddCondition(AnimatorConditionMode.Equals, 0, "state");

        // Apply to Scene Object
        MariangulaAI ai = Object.FindObjectOfType<MariangulaAI>();
        if (ai != null)
        {
            SpriteRenderer sr = ai.GetComponentInChildren<SpriteRenderer>();

            if (sr == null)
            {
                // Como es un greybox, probablemente tenga un MeshRenderer. No podemos poner un SpriteRenderer
                // y un MeshRenderer en el mismo objeto. Creamos un hijo visual.
                GameObject visualObj = new GameObject("Visuals");
                visualObj.transform.SetParent(ai.transform);
                visualObj.transform.localPosition = new Vector3(0, 0, -0.1f); // Ligeramente al frente
                visualObj.transform.localRotation = Quaternion.identity;

                sr = visualObj.AddComponent<SpriteRenderer>();

                // Ocultar el cubo original
                MeshRenderer mr = ai.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }

            // Asegurarnos de obtener el Animator que está en el MISMO objeto que el SpriteRenderer
            Animator animator = sr.GetComponent<Animator>();
            if (animator == null) animator = sr.gameObject.AddComponent<Animator>();

            // Limpiar cualquier Animator residual en el padre (creado por error en ejecuciones anteriores)
            Animator parentAnimator = ai.GetComponent<Animator>();
            if (parentAnimator != null && parentAnimator != animator)
            {
                Object.DestroyImmediate(parentAnimator);
            }

            animator.runtimeAnimatorController = controller;
            sr.sprite = idleSprites[0];

            ai.animator = animator;
            ai.spriteRenderer = sr;

            EditorUtility.SetDirty(ai);
            Debug.Log("¡Mariangula configurada con éxito en la escena! Las animaciones y el controlador se han creado automáticamente.");
        }
        else
        {
            Debug.LogWarning("Controlador y animaciones creados en " + animFolder + ", pero no se encontró el objeto Mariangula_Greybox (o no tiene el script MariangulaAI) en la escena actual para asignarlos automáticamente.");
        }

        AssetDatabase.SaveAssets();
    }

    private static AnimationClip CreateClip(Sprite[] sprites, float frameRate, bool loop)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = frameRate;
        
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i / frameRate;
            keyframes[i].value = sprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop; // Aplicamos el valor de loop
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }
}
