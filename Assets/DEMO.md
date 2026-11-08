# Demo: Ruinas del Eco

La demo es el primer hito jugable del proyecto. Usa una ambientación y un recorrido originales; Hollow Knight es únicamente una referencia de género y no se reutiliza su contenido protegido.

## Abrir y generar

1. Abre el proyecto con Unity `6000.3.26f1`.
2. Abre `Assets/Scenes/RuinsDemo.unity` para jugar la demo.
3. Abre `Assets/Scenes/CanalCampanas.unity` para jugar la primera zona del mapa.
4. Abre `Assets/Scenes/InventarioDemo.unity` para probar la mochila, intercambiar objetos con el almacén, usar pociones y recorrer la galería de animaciones.
5. Abre `Assets/Scenes/TiendaDemo.unity` para comprar y vender armas y objetos.
6. Abre `Assets/Scenes/EfectosDemo.unity` para explorar y reproducir 30 efectos especiales.
7. Si necesitas regenerar las escenas, usa las opciones del menu **Demo** para generar la demo principal, el Canal, las escenas de inventario/tienda o la galeria de efectos. Las opciones **Demo > Validar...** comprueban sus componentes y referencias.
8. Las escenas de demostracion se incluyen en Build Settings y `SampleScene` se conserva.

## Canal de las Campanas

- La entrada comienza sin Dash. La ruta principal es continua; los dos ramales elevados se recorren con el salto normal y son opcionales.
- Derrota al Guardian de la Estacion y recoge el cristal que deja para desbloquear Dash. La validacion del encuentro esta en **Demo > Validar jefe: Guardian de la Estacion**.
- La estacion de retorno antes del guardian se activa al tocarla: guarda la zona y posicion, activa la estacion y restaura la salud. El guardado sobrevive a reiniciar el juego.
- Al perder toda la salud, reapareces en el ultimo punto activado con salud completa; si aun no activaste uno, vuelves a la entrada de la zona. Monedas, llaves, armas, objetos, habilidades y estaciones activadas se conservan; se reinician el movimiento, dash, ataque, defensa y sus enfriamientos. No se pierde moneda por morir.
- La barrera de resonancia de la salida solo se atraviesa con un Dash horizontal. Al cruzarla, la conexion señalizada conduce de forma real y bidireccional a la **Galeria de Valvulas** (`Assets/Scenes/GaleriaValvulas.unity`).
- El atajo de regreso aparece junto a la barrera y devuelve a la entrada despues de derrotar al guardian.
- Para regenerar la escena o comprobar sus rutas, usa **Demo > Generar zona: Canal de las Campanas** y **Demo > Validar zona: Canal de las Campanas**.
- La escena reutiliza `ZonaMetroidvania2D` como raiz: identificador, nombre, limites de camara, entrada y conexion de salida.
- El Guardian de la Estacion tiene dos fases: espera el aviso naranja y salta sobre su golpe; cuando el indicador se vuelve verde, entra en sobrecarga y alterna el golpe con una onda azul horizontal que puedes esquivar saltando. Ataca durante su recuperacion. El cristal activa Dash y el atajo de regreso queda disponible tras la derrota.

## Galeria de Valvulas (Zona 5)

- La segunda zona del mapa metroidvania. Conecta bidireccionalmente con el Canal de las Campanas (`Assets/Scenes/CanalCampanas.unity`).
- **Transicion real bidireccional**: interactuar en la salida del Canal tras la barrera de Dash carga la entrada de la Galeria; la conexion oeste de la Galeria devuelve a Sira al extremo este del Canal.
- **Prueba de Dash**: un foso profundo sobre el vacio con una barrera de resonancia que requiere ejecutar un Dash horizontal en el aire para alcanzar la pasarela opuesta.
- **Trampas de Vapor (`ObstaculoVapor2D`)**: tres valvulas de escape que alternan en ciclos predecibles (reposo, advertencia/silbido y erupcion a presion). Durante la erupcion, causan dano a la integridad y empujan a Sira.
- **Atajo de Compuerta Hidraulica (`CompuertaAtajo2D`)**: un tunel inferior directo hacia la entrada que inicialmente se encuentra sellado por una pesada compuerta metalica. Al recorrer la zona y alcanzar la valvula de derivacion interior, Sira acciona el mecanismo y eleva la compuerta permanentemente. El desbloqueo persiste en `ProgresoPartida2D`.
- **Campana Elastica y Pogo**: situada bajo una cornisa elevada. Ejecutar un ataque hacia abajo (pogo) sobre la campana activa su resonancia, impulsa a Sira verticalmente con fuerza elastica aumentada y permite acceder a una repisa secreta con 3 Ecos.
- **Puntos de descanso y salida**: cuenta con su propia estacion de descanso (`PuntoControlZona2D`) para guardar y reponer salud, y un marcador de conexion ascendente que anticipa la siguiente zona: los **Talleres de Aire**.
- Para regenerar o validar la zona, usa **Demo > Generar zona: Galeria de Valvulas** y **Demo > Validar zona: Galeria de Valvulas**.

## Puntos de control

- **Demo > Validar puntos de control y reaparicion** comprueba guardado y recarga del checkpoint, estación y Dash; derrota y reaparición con salud completa; limpieza del movimiento temporal; conservación del inventario; fallback sin checkpoint, escenas no incluidas en Build Settings y recuperación de un checkpoint obsoleto. La prueba restaura el progreso previo del Editor.
- `PuntoControlZona2D` se puede añadir a una estación con `Collider2D` de trigger. Asigna un `puntoReaparicion`, un ID estable y, dentro de una zona, referencia a `ZonaMetroidvania2D`.
- `GestorReaparicion2D` registra al jugador, recupera el último descanso al cambiar de escena y vuelve a su escena si la derrota ocurre en otra zona. Cada escena de zona debe incluir al jugador, `VidaJugador2D` y un `PuntoEntradaZona2D` con el ID usado por su conexión. Incluye cada escena de zona en **File > Build Profiles > Scene List** (o Build Settings en versiones anteriores): los checkpoints de escenas ausentes no se guardan y un checkpoint antiguo se descarta al reaparecer, usando el inicio de la zona actual.

## Controles

- **A/D** o flechas: moverse.
- **Espacio**: saltar; la altura varía según cuánto se mantenga pulsado.
- **Mayús izquierdo**: dash (se desbloquea al recoger el cristal azul).
- **Intro** o clic izquierdo: ataque con espada (horizontal por defecto).

- **W / Flecha arriba + Ataque**: ataque vertical hacia arriba (para enemigos aereos).

- **S / Flecha abajo en el aire + Ataque**: ataque vertical hacia abajo (**Pogo / Nail Bounce**). Si impacta contra un enemigo, pinchos de sal o campanas elasticas, hace rebotar a Sira verticalmente hacia arriba y reinicia inmediatamente el enfriamiento del Dash.
- **F**: **Concentracion de Resonancia (Focus)**. Manten pulsado en el suelo para canalizar energia acustica acumulada al golpear enemigos (33 pts) y reparar 1 punto de integridad. Si recibes dano, la concentracion se interrumpe.
- **S / Flecha abajo en el aire + Dash**: **Descarga de Presion (Ground Pound)** (tras desbloquear la habilidad). Sira se precipita en picado a alta velocidad hacia el suelo, provocando una onda expansiva de dano sismico que fractura y destruye sellos de sal fragiles.
- **Peligros y Respawn seguro**: caer sobre pinchos o vapor sin pogo resta 1 punto de integridad y devuelve a Sira inmediatamente al ultimo suelo firme seguro con invulnerabilidad temporal.
- **C**: defenderse.
- **E / Y del mando**: interactuar con puertas y personajes cercanos. Al acercarte aparece una indicacion contextual.
- **Intro / Aceptar** durante un dialogo: revelar la linea que se esta escribiendo o avanzar.
- **Esc / Atras** durante un dialogo: cerrarlo. Si no hay dialogo, Esc abre la pausa como antes.
- **Tab**: abrir el mapa de la zona (solo muestra tramos recorridos).
- **Esc**: pausa; las flechas o WASD y Enter navegan el menú.
- **Mando**: stick izquierdo mueve; Start abre/cierra pausa (durante un diálogo, lo cierra); Select abre el mapa; el stick/D-pad y los botones de aceptar/volver navegan los menús.
- **Ratón**: usar los botones de inventario, intercambio, tienda y galería.
- `Jugador2D` clona el mapa de acciones `Player` al inicializarse para mantenerlo independiente del mapa `UI`, que utiliza la interfaz.

La interfaz de juego muestra integridad, ecos, llaves y armas; también incluye pausa, volumen general, resolución, pantalla completa y reducción de destellos. El HUD y los paneles usan `CanvasScaler` para adaptarse a la resolución. El mapa persiste únicamente las celdas que Sira ya ha recorrido; no dibuja conexiones ni zonas desconocidas. Tras caer, la pantalla de derrota indica el regreso automático al último punto de descanso; al completar la demo, la pantalla de victoria permite volver a intentarla.

Usa **Demo > Validar interfaz de juego** para comprobar la persistencia del mapa descubierto y las vinculaciones de teclado y mando del menú.

## Personajes y dialogos

- El generador actualizado coloca a Teren junto a la entrada del Canal con un marcador visual provisional. Si ya tenias la escena generada, usa **Demo > Generar zona: Canal de las Campanas** para incorporarlo; guarda primero cualquier cambio manual de esa escena. Acercate y pulsa **E** (o **Y** en el mando); no bloquea el paso.
- El texto aparece gradualmente. **Intro / Aceptar** muestra de inmediato la linea completa si aun se esta escribiendo; una segunda pulsacion avanza. **Esc / Atras** cierra la conversacion.
- Si el personaje, el jugador o la interfaz no estan disponibles, la conversacion no se abre y el control no se bloquea. Los dialogos no alteran el progreso ni los sucesos de la historia.
- `PersonajeSecundario2D` se puede agregar a un personaje con `Collider2D` en modo `Is Trigger`. Elige Mara, Teren, Narel o El Medidor; deja vacias las lineas personalizadas para usar el texto aprobado, o escribe reemplazos y ajusta la velocidad de texto en el Inspector.
- El catalogo incluye conversaciones breves de Mara y Narel para sus escenas futuras. No se colocan en el Canal, y el Medidor conserva mensajes de protocolo porque no es consciente ni habla.
- Usa **Demo > Validar interaccion y dialogos** para comprobar los dialogos aprobados, los indicadores y el comportamiento seguro si falta la interfaz.

El ataque de espada detecta objetivos dentro de su alcance y en la dirección que mira Sira. Cada enemigo recibe como máximo un impacto por ataque; los golpes válidos producen retroceso e invulnerabilidad breve. El jugador tiene un tiempo de recuperación entre ataques y recibe retroceso y un parpadeo visual al recibir daño.

Para comprobar estos casos en el Editor, usa **Demo > Validar combate cuerpo a cuerpo**. La prueba cubre ambos tipos de enemigo, varios colliders por objetivo, tiempo de recuperación, límites de alcance y altura, dirección, daño inválido, invulnerabilidad y retroceso.

## Objetivo

Explora el sendero y recoge el cristal azul. La barrera de prueba, situada despues del cristal, solo se abre durante un dash horizontal: mantén Mayús y pulsa izquierda o derecha para cruzarla. Caminar hacia la barrera, saltar por encima o hacer un dash vertical no la abre. El dash interrumpe el ataque y evita recibir daño durante su duración. La habilidad permanece al reaparecer en el punto de control. Después de la barrera, recoge ecos, consigue la llave antigua, abre el umbral y atraviesa el portal. Hay tres enemigos con diferencias de velocidad, resistencia y distancia de ataque. El guardado del inventario de la demo usa una clave separada de otros inventarios.

El catálogo de habilidades de exploración se encuentra en `HabilidadesExploracion2D`; ahora solo registra el Dash aprobado en el diseño. La habilidad se desbloquea y consulta con la misma API (`DesbloquearHabilidad` / `TieneHabilidad`), mientras movimiento, combate y bloqueos consultan su estado. **Demo > Validar habilidad de exploracion: Dash** comprueba el desbloqueo, la interacción con movimiento y combate y que la barrera no ceda al caminar, con Dash vertical o al terminar el impulso. Añade otra entrada únicamente cuando se actualice y apruebe el diseño del juego, junto con su comportamiento, obstáculo y pruebas.

La escena de inventario comparte una mochila guardada entre las dos escenas nuevas, incluye un almacén persistente para mover llaves, armas y objetos, y permite consumir pociones para recuperar vida. El botón de práctica permite probar la poción sin enemigos. Su galería reproduce los 35 clips de movimiento y combate incluidos para el jugador. La tienda vende armas y objetos por monedas; permite venderlos por la mitad de su precio de compra, pero protege las llaves para evitar perder el acceso a puertas.

La galeria `EfectosDemo` utiliza 30 efectos originales de Cartoon FX Remaster Free / JMO: tajos, explosiones, impactos, energia elemental, magia, naturaleza y agua. Cada boton reproduce una muestra completa con sus sistemas anidados, materiales, luces y estelas; los efectos continuos quedan activos para compararlos. La escena usa Bloom y viñeta de URP. Los prefabs de demostracion se guardan en `Assets/Demo/Prefabs/Efectos/` y contienen instancias anidadas de los prefabs originales, que permanecen en `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/`.

Para crear un enemigo sencillo, añade `EnemigoBasico2D` a un objeto con `Rigidbody2D`, `Collider2D` y `SpriteRenderer`. El enemigo patrulla automaticamente a ambos lados de su posicion inicial; puedes ajustar la distancia o asignar los transforms opcionales `limiteIzquierdo` y `limiteDerecho`. Configura `rangoDeteccion`, `rangoAtaque`, velocidad y vida en el Inspector. Pon al enemigo en la capa Enemy incluida en el alcance de ataque del jugador: el ataque de espada le aplica daño y retroceso. En el gizmo de la escena, el amarillo marca la patrulla, el cian la deteccion y el rojo el alcance de ataque. El jugador debe tener `VidaJugador2D` para recibir el daño del enemigo.

## Cinco arquetipos de enemigos

Usa **Demo > Generar demo de cinco enemigos** para crear y abrir `Assets/Scenes/EnemigosPrototipoDemo.unity`. La escena aparece tambien en Build Settings. Los cinco prefabs editables se guardan en `Assets/Demo/Prefabs/Enemigos/`; cada uno incluye su `Rigidbody2D`, collider, `SpriteRenderer` y script. El arte y los colores son provisionales y los valores de deteccion, vida, ataque, anticipacion y recuperacion se pueden cambiar en el Inspector. Usa **Demo > Validar demo de cinco enemigos** para comprobar la escena y la ruta del rondador.

Todos comparten los estados patrullar, detectar, aviso de ataque, atacar, recuperarse, recibir dano y derrotado. El aviso se representa con un tinte naranja y un cambio de escala; el HUD muestra el estado y la vida. El gizmo cian marca deteccion y el rojo el alcance de ataque. Los ataques tienen una anticipacion configurable y una pausa de recuperacion para que el jugador pueda reaccionar.

| Arquetipo | Comportamiento y respuesta | Como probarlo |
|---|---|---|
| Terrestre - Perseguidor | Patrulla en horizontal, persigue al jugador y ataca cuerpo a cuerpo. | Acercate para atraerlo; espera el aviso naranja y salta o retrocede antes del golpe. |
| Tirador - Vigia | Se mantiene a distancia y lanza un proyectil visible, lento y esquivable. | Entra en su rango, esquiva el proyectil y aprovecha la recuperacion para atacar con la espada. |
| Rondador - Guardaplataforma | Recorre cuatro puntos alrededor de la plataforma, con collider de trigger para no quedarse bloqueado por la geometria. | Observa su circuito; espera el aviso, cruza su trayectoria con un salto y golpealo al pasar. |
| Volador - Picador | Flota y, tras fijar la posicion del jugador, se lanza hacia ese punto. | Muevete lateralmente despues del aviso para salir del punto fijado y ataca cuando frene. |
| Resonador - Saltador | Salta ritmicamente y proyecta una onda horizontal de alcance limitado. | Salta sobre la onda durante su recuperacion y acercate para golpearlo. |

El jugador de prueba usa A/D o flechas para moverse, Espacio para saltar e Intro o clic izquierdo para atacar. Los proyectiles desaparecen al alcanzar al jugador, una superficie de la capa Ground o su limite de alcance. Las pruebas automatizadas del Editor estan en **Demo > Validar cinco arquetipos de enemigos**; verifican el aviso, la ejecucion de cada ataque, el dano, la invulnerabilidad, la derrota y el disparo de proyectiles.

Estas escenas usan un perfil de demostración independiente de la partida de las Ruinas del Eco. El contenido inicial se carga una sola vez; las compras, ventas e intercambios se guardan entre escenas. Esta entrega sigue siendo una demo para probar los sistemas antes de definir el alcance del juego completo.
