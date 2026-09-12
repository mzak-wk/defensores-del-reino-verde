# Resumen de Implementación: Sistema de Habilidades, HUD Dinámico y Cut-In Definitiva

Hemos completado e integrado todo el núcleo de programación para el nuevo sistema de combate, controles remapeables y cinemática de Ultimate estilo **Persona 5**.

---

## 1. Archivos Implementados y Modificados

### A. Sistema de Controles y Remapeo
- [`KeybindManager.cs`](file:///c:/Users/Cilindro/Github%20Projects/defensores-del-reino-verde/Assets/Scripts/KeybindManager.cs):
  - Singleton persistente (`DontDestroyOnLoad`).
  - Mapeo de `CombatAction`:
    - `BasicAttack` -> Mouse0 (Click Izquierdo).
    - `Skill1` -> Tecla 1 (`Alpha1`).
    - `Skill2` -> Tecla 2 (`Alpha2`).
    - `Ultimate` -> Tecla 3 (`Alpha3`).
  - Persistencia automática en `PlayerPrefs` y evento `OnKeybindChanged` para que el HUD actualice las etiquetas al instante si el jugador reconfigura las teclas en el menú de opciones.
  - Método `GetDisplayString(action)` que devuelve `"CLICK"`, `"1"`, `"2"`, `"3"`, etc.

### B. Datos de Armas Escalables
- [`WeaponData.cs`](file:///c:/Users/Cilindro/Github%20Projects/defensores-del-reino-verde/Assets/Scripts/Weapons/Data/WeaponData.cs):
  - Añadidos slots de iconos para cada habilidad: `basicIcon`, `skill1Icon`, `skill2Icon`, `ultimateIcon`.
  - Getters unificados y alias compatibles con los nombres de habilidades (`Skill1Name`, `Skill2Name`, `UltimateName`, etc.).

### C. HUD de Habilidades (Abajo a la Derecha)
- [`AbilityHUDController.cs`](file:///c:/Users/Cilindro/Github%20Projects/defensores-del-reino-verde/Assets/Scripts/UI/AbilityHUDController.cs):
  - Control de 4 slots: Básico, Habilidad 1, Habilidad 2, Definitiva.
  - Iconos dinámicos que se actualizan automáticamente al equipar o cambiar de arma.
  - Sombreado grisáceo (`CanvasGroup.alpha = 0.55f`) y relleno radial de 360 grados (`Image.fillAmount = remaining / total`) durante el tiempo de recarga (cooldown).
  - Texto numérico en segundos (`4s`, `0.5s`) cuando está en recarga.
  - Texto dinámico de la tecla asignada.
  - Método `SetHUDVisible(bool visible)` compatible con el menú de Opciones para ocultar/mostrar todo el HUD y guardarlo en `PlayerPrefs ("ShowAbilityHUD")`.

### D. Cinemática Persona 5 Cut-In
- [`UltimateCutInController.cs`](file:///c:/Users/Cilindro/Github%20Projects/defensores-del-reino-verde/Assets/Scripts/UI/UltimateCutInController.cs):
  - Pausa el tiempo del juego (`Time.timeScale = 0f`).
  - Invulnerabilidad temporal para Andrius (`BarraVida.SetInvulnerable(true)`).
  - Reproduce en tiempo real no escalado (`Time.unscaledDeltaTime`) el rasgado del papel de `Ulti.png`.
  - La máscara contiene la mirada de Andrius y realiza un paneo horizontal hacia la izquierda (`slideDistanceX`) para dar sensación de velocidad y drama anime.
  - Al culminar la animación: restaura `Time.timeScale = 1f`, quita la invulnerabilidad y ejecuta el disparo de la habilidad definitiva del arma.

### E. Conexión en el Jugador
- [`PlayerCombat.cs`](file:///c:/Users/Cilindro/Github%20Projects/defensores-del-reino-verde/Assets/Scripts/PlayerCombat.cs):
  - Removidas teclas estáticas (`KeyCode.X, C, V, Mouse0`) a favor de `KeybindManager.Instance.GetKey(...)`.
  - Integrada la llamada al `UltimateCutInController.Instance.PlayCutIn(...)` antes de lanzar la definitiva del arma activa.
  - Implementados `GetCooldownRemaining(action)` y `GetTotalCooldown(action)` para alimentar la UI del HUD en tiempo real (incluyendo el sistema de sobrecalentamiento del Proyector de Reforestación).

---

## 2. Pasos Rápidos en Unity para el Ensamble Visual

### 1. Configuración de `Ulti.png` en Unity Inspector:
1. Selecciona `Assets/sprites(r)/UI/Ulti.png`.
2. En el Inspector:
   - **Texture Type**: `Sprite (2D and UI)`
   - **Sprite Mode**: `Multiple`
   - **Filter Mode**: `Point (no filter)`
   - **Compression**: `None`
3. Abre el **Sprite Editor**, recorta los frames del papel rasgándose y dale a **Apply**.

### 2. Creación del GameObject del Cut-In (`UltimateCutInController`):
1. En tu `Canvas` principal, crea un GameObject vacío llamado `UltimateCutIn`:
   - Agrega el script `UltimateCutInController.cs`.
   - Como hijo, crea una `Image` llamada `PaperTear_Mask` con el componente `Mask` de Unity y asígnale el primer sprite del papel.
   - Como hijo de `PaperTear_Mask`, crea una `Image` llamada `CharacterEyes` donde irá el sprite de la mirada de Andrius.
   - Arrastra las referencias correspondientes al script en el Inspector (`paperSprites`, `eyesContainer`, `eyesImage`, `cutInRoot`).

### 3. Creación del HUD de Habilidades (`AbilityHUDController`):
1. En el `Canvas`, en la esquina inferior derecha (Anchor: Bottom-Right), crea un contenedor `AbilityHUD` y añade `AbilityHUDController.cs`.
2. Crea los 4 slots con:
   - `IconImage`
   - `CooldownOverlay` (Image de color negro/gris semitransparente, **Image Type**: `Filled`, **Fill Method**: `Radial 360`)
   - `KeybindText` (TextMeshPro con "CLICK", "1", "2", "3")
   - `CooldownText` (TextMeshPro para los segundos restantes)
3. Conecta cada ranura al script en el Inspector.
