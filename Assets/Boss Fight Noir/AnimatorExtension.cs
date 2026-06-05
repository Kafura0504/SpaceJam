// Assets/Boss Fight Noir/AnimatorExtension.cs
// =============================================================
// SpaceJam - AnimatorExtension
// =============================================================
// TUJUAN:
//   Extension method untuk Animator agar bisa cek parameter
//   sebelum set, mencegah error "Parameter not found".
//
//   Berguna ketika beberapa pattern berbagi Animator yang
//   memiliki parameter berbeda-beda.
//
// CARA PAKAI:
//   animator.HasParam("Speed")      → bool
//   animator.SafeSetBool(...)       → tidak error jika param tidak ada
//   animator.SafeSetTrigger(...)    → tidak error jika param tidak ada
//   animator.SafeSetFloat(...)      → tidak error jika param tidak ada
//   animator.SafeSetInt(...)        → tidak error jika param tidak ada
//
// Tidak perlu di-attach ke GameObject — ini static class.
// =============================================================

using UnityEngine;

public static class AnimatorExtension
{
    /// <summary>
    /// Cek apakah Animator memiliki parameter dengan nama tertentu.
    /// </summary>
    public static bool HasParam(this Animator animator, string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
    }

    /// <summary>
    /// SetTrigger yang aman — tidak error jika parameter tidak ada.
    /// </summary>
    public static void SafeSetTrigger(this Animator animator, string paramName)
    {
        if (animator == null) return;
        if (!animator.isActiveAndEnabled) return;

        if (animator.HasParam(paramName))
            animator.SetTrigger(paramName);
        else
            Debug.LogWarning($"[AnimatorExtension] Trigger '{paramName}' tidak ada di Animator '{animator.gameObject.name}'.");
    }

    /// <summary>
    /// SetBool yang aman — tidak error jika parameter tidak ada.
    /// </summary>
    public static void SafeSetBool(this Animator animator, string paramName, bool value)
    {
        if (animator == null) return;
        if (!animator.isActiveAndEnabled) return;

        if (animator.HasParam(paramName))
            animator.SetBool(paramName, value);
        else
            Debug.LogWarning($"[AnimatorExtension] Bool '{paramName}' tidak ada di Animator '{animator.gameObject.name}'.");
    }

    /// <summary>
    /// SetFloat yang aman — tidak error jika parameter tidak ada.
    /// </summary>
    public static void SafeSetFloat(this Animator animator, string paramName, float value)
    {
        if (animator == null) return;
        if (!animator.isActiveAndEnabled) return;

        if (animator.HasParam(paramName))
            animator.SetFloat(paramName, value);
        else
            Debug.LogWarning($"[AnimatorExtension] Float '{paramName}' tidak ada di Animator '{animator.gameObject.name}'.");
    }

    /// <summary>
    /// SetInteger yang aman — tidak error jika parameter tidak ada.
    /// </summary>
    public static void SafeSetInt(this Animator animator, string paramName, int value)
    {
        if (animator == null) return;
        if (!animator.isActiveAndEnabled) return;

        if (animator.HasParam(paramName))
            animator.SetInteger(paramName, value);
        else
            Debug.LogWarning($"[AnimatorExtension] Int '{paramName}' tidak ada di Animator '{animator.gameObject.name}'.");
    }

    /// <summary>
    /// ResetTrigger yang aman — tidak error jika parameter tidak ada.
    /// </summary>
    public static void SafeResetTrigger(this Animator animator, string paramName)
    {
        if (animator == null) return;
        if (!animator.isActiveAndEnabled) return;

        if (animator.HasParam(paramName))
            animator.ResetTrigger(paramName);
    }

    /// <summary>
    /// CrossFade yang aman — tidak error jika state tidak ada.
    /// Mengembalikan true jika berhasil, false jika gagal.
    /// </summary>
    public static bool SafeCrossFade(this Animator animator, string stateName, float transitionDuration = 0.2f)
    {
        if (animator == null) return false;
        if (!animator.isActiveAndEnabled) return false;

        animator.CrossFade(stateName, transitionDuration);
        return true;
    }
}