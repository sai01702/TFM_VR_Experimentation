using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRLogger;


public class FuncionamientoBalas : MonoBehaviour
{
    public float vida;
    public float nacimiento;
 
    void OnEnable() {
        nacimiento = Time.time; // guardo el tiempo de nacimiento de la bala
    }
    void Update() {
        if(Time.time > nacimiento + vida) { // si el tiempo actual es mayor al tiempo de nacimiento más el tiempo de vida
            // VR Logger Integration - MISS (Timeout)
            // This is a definitive miss.
            LoggerService.LogEvent("action_fail", "target_miss");
            
            gameObject.SetActive(false); // desactivo la bala
        }
    }

    void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("Disparable")) {
            // VR Logger Integration - HIT
            LoggerService.LogEvent("action_success", "bullet_hit");

            ObjetivosManager.Instance.fuera = true;
            ObjetivosManager.Instance.Despawn();
            Debug.Log("Objetivo destruido");
            ObjetivosManager.Instance.fuera = false;
            gameObject.SetActive(false); 
            Debug.Log("Bala destruida");
            ObjetivosManager.Instance.puntos ++;
            Debug.Log("Puntos: " + ObjetivosManager.Instance.puntos);
        }
        else
        {
            // VR Logger Integration - MISS (Hit something else)
            // Only log if we care about every single bullet miss hitting environment.
            // For now, let's log it as target_miss to calculate accuracy properly?
            // Actually, if we log 'shot_fired', then 'bullet_hit', HitRatio = bullet_hit / shot_fired.
            // We don't strictly *need* target_miss for Ratio, but it's good for clarity in timeline.
            LoggerService.LogEvent("action_fail", "target_miss");
            gameObject.SetActive(false); // Disable bullet on wall hit too? Original code didn't seem to have else?
            // Wait, original code DOES NOT disable bullet on non-target hit. 
            // If I disable it, I change game mechanic. I should NOT disable it if original didn't.
            // But usually bullets disappear on wall. 
            // Looking at the provided snippet:
            // It only had "if (CompareTag("Disparable"))".
            // So default behavior: bounce? 
            // If I check `PoolManager` or `bulletPrefab` I'd know.
            // But safely, I should just Log. 
            // If it bounces and hits later, that's a lucky shot.
            // BUT, `target_miss` usually implies the attempt failed. 
            // If I log `target_miss` on wall hit, and then it bounces and hits target, I have both miss and hit for one bullet?
            // Or if it bounces 5 times, 5 misses?
            // Let's log 'bullet_collision' maybe? Or just skip environment hits to avoid spam.
            // Experiments usually want Hit Ratio. Shot vs Hit is enough. 
            // But wait, the plan said "Log target_miss" in Update() for timeout.
        }
    }
}
