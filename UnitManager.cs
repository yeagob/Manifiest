/********************************************************************************************
* UnitManager.cs
*
* Todas las unidades de a pie: policias, manifestantes y peatones, llevan este script
* y se encarga de darles las propiedades a cada tipo de unidad. Estas propiedades
* abarcan desde el tipo, los estados, los objetos en mano, las acciones en marcha, etc.
*
* Durante el Update analizamos las personas alrededor y el contexto, para actualizar los 
* estados y acciones de cada unidad.  
*
* (cc) 2014 Santiago Dopazo 
*********************************************************************************************/

using UnityEngine;
using System.Collections;

public class UnitManager : MonoBehaviour {
	//Definimos si los valores de la unidad se rellenaran de forma aleatoria
	public bool aleatorio = false;

	//Variable principal de estado de las personas
	public float energia = 50;//0 a 100

	//Variables de estado de los manifestantes
	public float valor = 3;//-50 a 50
	public float activismo = 0; // 0 a 100

	//Variable de estado del peaton
	public float apoyo = -5;//-50 a 50// Inspired By: // © 2013 Brett Hewitt All Rights Reserved

	//Variable de estado de la policia, podriamos
	public float enfado = 15;//-50 a 50

	//Caracterisitcas personales 
	public string nombre;
	public string apellidos = "Sanchez Perez";
	public string creencias = "Ciencia";
	public float salario = 1000;
	public int edad = 30;
	public float prisa = 0.01f; 	
	public Texture2D cara;
	public float fuerza = 200f;//Fuerza de lanzamiento de objetos

	//Persona con la que venimos a la mani. Nuestro colega
	public GameObject empatiaPersona;

	//Persona/objeto con la que interactuara la unidad actual 
	public Transform objetivoInteractuar = null;

	//Control de 'objetos alrededor' de la unidad
	Collider[] objectsInRange = new Collider[0];		
	//Rango de escucha de la unidad
	public float rangoEscucha = 10.0f;

	//Estados del Ser, ¿que es esta unidad?
	public bool esLider = false;
	public bool esManifestante = true;
	public bool esPeaton = false;
	public bool esPeriodista = false;
	public bool esPolicia = false;
	public bool esCapitan = false;
	public bool esRepartidor = false;	//Pendiente de crear un objeto de mano/pamfletos

	//Control de objetos-en-mano
	public GameObject manoIzquierda;
	public GameObject manoDerecha;
	public GameObject manoVacia;

	//Estados si TIENE algun objeto importante
	public bool tieneMusica = false;
	public bool tieneSpray = false;
	public bool tienePancarta = false;
	public bool tieneMechero = false;
	public bool tieneEscudo = false;
	public bool tieneEscopeta = false;
	public bool tieneMovil = false;
	public bool tienePanfleto = false;
	public bool tieneFlor = false;
	public bool tieneOVNI = false;//Objeto arrojadizo

	//Estados del manifestante
	public bool estaCantando = false;
	public bool estaParado = false;
	public bool estaAgachado = false;
	//Si un policia ve al manifestante comenter un delito
	public bool estaFichado = false;
	//Si cae KO un manifestante fichado, es detenido
	public bool estaKO = false;
	public bool estaPintando = false;
	public bool estaAtacando = false;
	public bool estaBailando = false;
	public bool estaEnActoPoetico = false; //Acciones especiales
	public bool estaReproduciendoMusica = false;
	public bool estaHerido = false;
	public bool estaOcupado = false;//Peatones que no se uniran a la mani aunque quieran

    //Estados circunstanciales
	public bool recibeImpacto = false;
	public bool moveToAttack = false;
	public bool terceraPersona = false;
	public bool arrojandoObjeto = false;

	//Tiempos
	public float tiempoExistencia = 200; // Tiempo que pasa antes de desaparecer
	public float tiempoEnAparecer = 0; //Variable usada para las intros
	private float tiempoCantando = 0;
	private float tiempoBailando = 0;
	private float tiempoSolo = 0;
	private float tiempoAturdido = 0;
	private float tiempoVideo = 0;
	public float tiempoCorriendo = 0; 
	public float tiempoParado = 0f; 

	// Use this for initialization
	void Start () {

		//Asigancionn de valores aleatorios para peatones.
		if (aleatorio) {		
			energia = Random.Range(10,100);//10 a 100
			valor = Random.Range(1,50);//1 a 50
			apoyo = Random.Range(0,100);//0 a 100
			
			salario = Mathf.RoundToInt(Random.value * 2500) + 400;
			edad = Mathf.RoundToInt(Random.value * 50) + 15;
			//Un tercio de los peatones jamas se parara, porque esta ocupado
			estaOcupado = (Mathf.RoundToInt(Random.value * 3) == 1) ? true : false ;

		}

		//Creamos un puntero al animator
		Animator anim = GetComponent<Animator>();

		//Velocidad al Caminar, de los peatones, apronximadamente igual.
		if (prisa > 0) {
			prisa = (Mathf.RoundToInt(Random.value * 5) + 1)/100f;
			if (prisa < 0.2f) prisa = 0.2f;		
		}
		if (!estaParado && !terceraPersona)
			anim.SetFloat("Speed",prisa);

		//*******************************************************************
		//Control desde el Mangaer del numero de unidades existentes. Desafio.
		//Esto posiblemente no haga falta
		Manager.temp.unidades.Add (this.gameObject);	

		//Control de cantidad de manifestantes/policias/peatones/conches
		if (esManifestante) {
			//Añadimos manifestante al manager
			Manager.temp.AddManifest ();
			Manager.temp.unidades.Add (this.gameObject);	
			/***********************
			 *   OBJETOS EN MANO
			 * ********************/
			ObjetoDeMano manoIz = manoIzquierda.GetComponent<ObjetoDeMano>();
			ObjetoDeMano manoDer = manoDerecha.GetComponent<ObjetoDeMano>();
			//Si tiene un objeto arrojadizo en una mano entonces tiene un OVNI
			tieneOVNI = (manoIz.esArrojable || manoDer.esArrojable);				
			tienePancarta = (manoDer.tag == "Pancarta" || manoIz.tag == "Pancarta" 
			                 || manoDer.name == "flag" || manoIz.name == "flag");				 
			tieneMechero = (manoDer.name == "Mechero Fuego" || manoIz.name == "Mechero Fuego");
			tieneMovil =  (manoDer.name == "Movil" || manoIz.name == "Movil");
			tieneSpray =  (manoDer.name == "Spray" || manoIz.name == "Spray");
			tieneMusica =  (manoDer.name == "Radio" || manoIz.name == "Radio");
			esRepartidor = (manoDer.name == "Panfletos" || manoIz.name == "Panfletos");
			esPeriodista = (manoDer.name == "Camara" || manoIz.name == "Camara"); 

		}
		else if (esPolicia) {
			//Añadimos policia al manager
			Manager.temp.AddPolicias();
			//Control de Objetos en Mano-Policias
			ObjetoDeMano manoIz = manoIzquierda.GetComponent<ObjetoDeMano>();
			ObjetoDeMano manoDer = manoDerecha.GetComponent<ObjetoDeMano>();
			tieneEscopeta =  (manoDer.name == "Escopeta" || manoIz.name == "Escopeta");
			tieneEscopeta =  (manoDer.name == "Escudo" || manoIz.name == "Escudo");
		}
		else if (esPeaton) 
			//Añadimos policia al manager
			Manager.temp.AddPeatones();


		//TEMP:
		//Para el video. Movemos a los manifestantes fuera de su lugar de aparicion. 
		/*if (!esPolicia)
			this.transform.Translate(new Vector3(50,0,0));*/
	}
	
	// Update is called once per frame
	void Update () {		
		//TEMP:
		//Para el video / Deberia crear un Script especifico de acciones: Inicial / Poetica, etc
		if (Time.timeScale > 0)
			tiempoVideo ++;

		//Guion de acciones para el video
		/*if (esManifestante) {
			if (tiempoVideo == (tiempoEnAparecer)) {			 
				this.transform.Translate(new Vector3(-50,0,0));
			}
			if (tiempoVideo == 35) {
				estaParado = false;
				GetComponent<Animator>().SetFloat("Speed",prisa);
			}
			if (tiempoVideo == 65) {
				estaCantando = true;
			}
			if (tiempoVideo == 215) {
				estaCantando = false;
			}
			if (tiempoVideo == 220) {
				estaParado = true;
			}
		}*/
			
		//creamos un puntero al Animator
		Animator anim = GetComponent<Animator>();

		//************************/
		/*ESTA CAMINANDO/CORRIENDO 
		//************************/			
		if (!estaParado) {
			//Si se cansa, se para. 
			if (valor < 0 && energia < -20) 
				isMoving(false);
		}
		
		//**************
		//TIEMPO CORRIENDO
		//**************			
		if (tiempoCorriendo > 0) {
			//Correr quita energia, pero aumenta el valor y el activismo
			energia -= 0.3f * Time.deltaTime;
			valor += 0.3f * Time.deltaTime;
			activismo += 0.1f * Time.deltaTime;
			tiempoCorriendo += Time.deltaTime;
			// Si esta muy cansado deja de correr y vuelve a andar
			if ((energia < 20 && valor > -30)) {
				anim.SetFloat("Speed",prisa);
				tiempoCorriendo = 0;
			}
			//Si esta muy cansado se para a descansar antes de caer KO
			if (energia < 5)
				isMoving(false);
			
		}
		else if (energia < 0) energia += 0.3f * Time.deltaTime;
		
		//**************
		//ESTA PARADO
		//**************
		if (estaParado && !terceraPersona) {
			//Detenemos la animacion de caminar
			anim.SetFloat("Speed",0);
			tiempoParado += Time.deltaTime;
			//Mientras esta parado, sube la energia, pero baja el activismo
			energia += 0.1f * Time.deltaTime;
			activismo -= 0.1f * Time.deltaTime;
		}
		else {			
			if (anim.GetFloat("Speed") == 0 && !terceraPersona) {
				//Iniciamos la animacion de caminar
				anim.SetFloat("Speed",prisa);
				tiempoParado = 0;
			}
		}

		//*****************
		//  SALIR CORRIENDO (va bien?)
		//*****************
		if ((valor < -20 && activismo < Manager.temp.activismoActivista) || (valor < -40 && activismo >= Manager.temp.activismoActivista) 
			    || (valor < 0 && apoyo < 50 && esPeaton) && tiempoCorriendo == 0) { //&& !esPolicia
			//Media vuelta y a correr
			transform.rotation.Set(0,180,0,0);			
			anim.SetFloat("Speed",1f);
			//El simple hecho de salir corriendo aumenta el valor de la persona
			valor += 20;
			//Indicamos que esta corriendo
			tiempoCorriendo += Time.deltaTime;
		}

		//********************
		//REPRODUCIENDO MUSICA
		//********************
		if (estaReproduciendoMusica) {			
			if (!GetComponent<AudioSource>().isPlaying) {
				//Reproducimos el audio
				GetComponent<AudioSource>().Play();
				//Iniciamos la animacion de agacharse a poner musica
				anim.SetBool("Agachado", true);
				estaParado = true;
				estaAgachado = true;
				//Aumenta el ambiente en la manifestacion
				Manager.temp.IncAmbiente(10);
			}
			else if (terceraPersona && estaAgachado) {
				anim.SetBool("Agachado", false);
				estaAgachado = false;
			}
		}
		else {
			if (tieneMusica) {
				GetComponent<AudioSource>().Stop();
				anim.SetBool("Agachado", false);
			}
		}

		//**************
		//ESTA BAILANDO
		//**************			
		if (estaBailando && !tieneMusica && !estaCantando) {
			//Si no estaba bailando iniciamos la animacion de bailar
			if (tiempoBailando == 0) 
				anim.SetBool("Bailando", true);

			tiempoBailando += Time.deltaTime;

			//Cuanto mas activista, mas tiempo bailara
			if (tiempoBailando < activismo) { 
				//Bailar sube la energia y el valor
				energia += 0.01f * Time.deltaTime;
				valor   += 0.05f * Time.deltaTime;	
			}
			else
				estaBailando = false;
		}
		else if (!estaBailando && tiempoBailando >0) {
				anim.SetBool("Bailando", false);
				tiempoBailando = 0;
		}

		//**************
		//ESTA CANTANDO
		//**************			
		if (estaCantando && !tieneMusica && !estaBailando) {
			//Se acaba de activar la opcion, iniciamos la acnimacion
			if (tiempoCantando == 0) {
				anim.SetBool("Protestando", true);
				try {
					//Iniciamos el audio de protesta
					//AudioSource.Clip = cancionActual;
					GetComponent<AudioSource>().Play();
				}
				catch{}
			}
			tiempoCantando += Time.deltaTime;
			//Cantar sube el valor y el activismo. La energia sube o baja dependiendo del activmismo
			valor += 0.03f * Time.deltaTime;	
			activismo += 0.03f * Time.deltaTime;	
			energia += ((activismo-50) / 100) * Time.deltaTime;

			//Si lleva tiempo cantando para
			if (tiempoCantando > energia + activismo)
				estaCantando=false;
		}
		else if (esManifestante) {
			//Si no esta cantando, detenemos la animacion y el audio
			if (!estaCantando && anim.GetBool("Protestando")){
				anim.SetBool("Protestando", false);
			try {
				//Desafio, creo por copia los audioSource?? y los destruyo?? o Asigno los clips??
				GetComponent<AudioSource>().Stop();	
			}
			catch{}
			}
		}

		//**************
		//ESTA ATACANDO
		//**************
		//La implemantacion de la accion se lleva a cabo cuando se analizan los objetos de alrededor, 
		//para aprobechar y buscar objetivos. Aqui solo las variaciones del estado
		if (estaAtacando) {
			//Si es muy activista le sube la energia, si no le baja
			energia += ((activismo-75) / 100) * Time.deltaTime; 
			valor += 0.02f * Time.deltaTime;
		}

		//*************************
		// DESAPARICION DE PEATONES
		//*************************
		//Si es peaton y esta mucho tiempo Solo, desaparace
		if (tiempoSolo > tiempoExistencia && esPeaton)
		{
			//Destruimos el peaton
			Manager.temp.unidades.Remove (this.gameObject);			
			Destroy (this.gameObject);
			//reducimos la cantidad de peatones
			Manager.temp.LessPeatones();
		}
	
		//**************
		//RECIVE UN IMPACTO
		//**************		
		if (recibeImpacto) {

			//Cualquier unidad que reciba un impacto pierde valor y energia.
			valor -= 10;
			energia -= 10;
		
			//Hay un 25% de posibilidades de quedar aturdido, si no es el lider, ese no se aturde.
			if (Mathf.Round(Random.value * 4) == 3 && !esLider)			
				tiempoAturdido = (150 - energia + valor)/5;

			//Si es peaton pierde mucho valor, energia y apoyo
			if (esPeaton) {
				valor -= 20;
				energia -= 20;
				apoyo -= 20;
			}

			//Si es manifestante pierde un poco mas que si es activista
			if (esManifestante) {
				valor -= 10;
				energia -= 10;
			}
							
			//Si es activista el valor y el activismo suben, la energia baja igual
			if (activismo >= Manager.temp.activismoActivista) {
				valor += 25;
				activismo += 5;
			}
			
			//Si es lider, solo pierde un poco de energia, el valor y el activismo suben
			if (esLider) {
				valor +=5;
				activismo += 5;
				energia +=5;
			}

			//Si es policia el enfado sube y si tiene escudo, sufre la mitad
			if (esPolicia) {
				enfado += 10;
				if (tieneEscudo) {
					valor += 5;
					energia += 5;
					tiempoAturdido = 0;
				}
			}

		 	//Hay un 25% de posibilidades de que la unidad quede herida.
			if (Mathf.Round(Random.Range(0, 4)) == 3 && !estaHerido) {
				estaHerido = true;
				//Añadimos al log el suceso
				Manager.temp.sucesos.Add (name + " ha resultado herido/a.");	

				if (esPolicia)
					Manager.temp.AddPoliciasHeridos();
				else
					Manager.temp.AddManifestantesHeridos();
			}

			recibeImpacto = false;
		}

		//**************
		//TIEMPO ATURDIDO
		//**************
		//El tiempo que la persona pasa ATURDIDA depende de la cantidad de energia que tenga y de lo enfadado que este
		if (tiempoAturdido > 0) {			
			tiempoAturdido -= Time.deltaTime;
			energia += 0.1f * Time.deltaTime;
			isMoving(false);
		}	

		//************
		//     K.O.
		//************
		if (energia < 0 && !estaKO) {
			if (esPeaton)
				GameObject.Destroy(this);
			//Animacion de caer KO
			anim.SetBool("KO", true);
			//Desactivamos todo movimiento
			isMoving(false);
			estaKO = true;
			//Damos un tiempo de recuperacion. 
			tiempoAturdido = 100;
			//Para evitar el loop en la animcion.
			if(!anim.IsInTransition(0)){
				try{
					anim.animation.Stop();				
				}
				catch{}
			}						
		}


		//############################### OBJECTS IN RANGE ################################

		//Detectamos los objetos alrededor de la unidad y la influencia sobre esta
		//Desafio: molaria acotar a objetos de las capas 8 Personas, 9 cohces, 16 furgones + contenedores

		//Personas alrededor. Esta accion se va a realizar 1/10 frames estadiasticamente
		if (Random.Range(0,10) == 0)
		/*-->*/	objectsInRange = Physics.OverlapSphere(transform.position, rangoEscucha, 1 << 8);
		
		//Si hay mas de 2 persona alrededor, tenemos en cuenta su influencia sobre esta unidad
		if (objectsInRange.Length > 2){	
			//Creamos una variable con del componenete ComportamientoHumano, para modificarla mas eficazmente
			ComportamientoHumano comportamientoH = GetComponent<ComportamientoHumano>();

			// Variables de influenciacion
			int cuantosParados = 0, cuantosCantando = 0, cuantosPolicias = 0, cuantosAtacando = 0, cuantasPancartas = 0, 
			cuantosManifestantes = 0, cuantosActosPoeticos = 0, cuantosBailando = 0, cuantaPrensa = 0;
			Transform objetivoCercano = null;
			bool liderCerca = false;
			bool suenaMusica = false;

			//Hacemos un analisis de lo que ocurre a nuestro alrededor.
			foreach (Collider persona in objectsInRange)  {			  			 
   			  try {
				//Analizamos el UnitManager de cada personaCercana a nuestro alrededor
				UnitManager personaCercana = persona.gameObject.GetComponent<UnitManager>();
				//Incrementamos los contadores de las circunstancias que pueden influir a la aunidad
				if (personaCercana.esManifestante) 
					cuantosManifestantes ++;							
				if (personaCercana.tienePancarta) 
					cuantasPancartas ++;
				if (personaCercana.estaParado) 
					cuantosParados ++;
				if (personaCercana.estaCantando) 
					cuantosCantando ++;
				if (personaCercana.estaAtacando) {
					cuantosAtacando ++;
					if (personaCercana.esManifestante && esPolicia && objetivoCercano == null) {
						//Identificamos al atacante mas cercano, a cada policia
						if (objetivoCercano == null) 
							objetivoCercano = persona.gameObject.transform;
						//Si encontramos un atacante mas cercano que el anterior, lo asignamos
						else if (Vector3.Distance(transform.position,objetivoCercano.position) > 
						         Vector3.Distance(transform.position, persona.gameObject.transform.position))
							objetivoCercano = persona.gameObject.transform;
					}
				}
				if (personaCercana.estaEnActoPoetico)
					cuantosActosPoeticos ++;
				if (personaCercana.esPeriodista)
					cuantaPrensa ++;
				if (personaCercana.estaBailando)
					cuantosBailando ++;
				if (personaCercana.esPolicia) {
					cuantosPolicias ++;
					//Identificamos al policia mas cercano, a cada manifestante
					if (esManifestante) {
						if (objetivoCercano == null) 
							objetivoCercano = persona.transform;
						//Si encontramos un policia mas cercano que el anterior, lo asignamos 
						else if (Vector3.Distance(transform.position,objetivoCercano.position) > 
							     Vector3.Distance(transform.position, persona.transform.position))
							objetivoCercano = persona.transform;
					}
				}
				if (!liderCerca && personaCercana.esLider) 
					liderCerca = true;
										
				if (!suenaMusica && personaCercana.estaReproduciendoMusica)
					suenaMusica = true;
			
				//Comportamiento especial solo para Repartidores. 
				if (this.esRepartidor) {
									
					//Si esta muy cerca de esta persona, le damos el panfleto
					if (Vector3.Distance(persona.transform.position,transform.position) <= 1 && !personaCercana.tienePanfleto) {
						//Hacemos que se miren 
						personaCercana.transform.LookAt(transform.position);
						transform.LookAt (personaCercana.transform.position);
						//Si es peaton le sube el 'apoyo', si es manifestante el 'activismo'	
						if (personaCercana.esPeaton)
							//A mayor salario y mayor edad, menor apoyo.
							personaCercana.apoyo+=(10/(personaCercana.salario/1000))/(edad/30);
						else if (personaCercana.esManifestante)
							personaCercana.activismo+=10;
						//Marcamos que esa persona ya tiene el panfleto
						personaCercana.tienePanfleto = true;
						//Aumenta la barra de conciencia local
						Manager.temp.IncConciencia(1);
						//Si la persona era objetivo, deja de serlo.
						if (objetivoInteractuar == persona.gameObject.transform)
								objetivoInteractuar = null;
					}
					else {
						//Si no tenia objetivo, asignamos uno y vamos hacia el.
						if (!objetivoInteractuar) {
							objetivoInteractuar = persona.gameObject.transform;
							comportamientoH.destino = objetivoInteractuar;
							comportamientoH.moviendose = true;
							isMoving(true);
						}
					}
				}

				}//end try/ Desafio: Eliminar este try.
				catch(UnityException e) {
					Debug.Log(name + ": ERROR - "+e.ToString() + " / En relacion a:" +persona.name);
				}
			  }	//Recuento de influencias finalizado.
			//Pasamos a aplicar los efectos sobre cada tipo de unidad

			
			//Si se ha definido un objetivo cercano, lo asignamos como objetivo con el que interactuar
			if(objetivoCercano)
				objetivoInteractuar	= objetivoCercano;


			//***************
			//   POLICIA
			//**************
			//Modificamos el estado de cada policia, en funcion de lo que ocurre a su alrededor
			if (esPolicia) {
					valor -= ((cuantosManifestantes + cuantosCantando + cuantosBailando - cuantosPolicias) / 100) * Time.deltaTime;
					enfado += ((cuantosManifestantes + cuantosCantando + cuantosBailando+ cuantosAtacando - cuantosPolicias) / 100) * Time.deltaTime;
					energia -= cuantasPancartas/ 100 * Time.deltaTime;				
			}

			//***************
			//    PEATON
			//**************
			//Modificamos el estado de cada peaton, en funcion de lo que ocurre a su alrededor
			if (esPeaton) {
				apoyo += (cuantosCantando + cuantasPancartas + cuantosBailando - cuantosPolicias) / (salario / (100 - edad)) * Time.deltaTime;
				valor -= cuantosAtacando * ((salario / 1000) + (edad / 100))* Time.deltaTime;
				energia += (apoyo * cuantosCantando) / 100 * Time.deltaTime;

				// Si no tiene manifestantes alrededor, incrementamos el contador de tiempoSolo, si no lo reiniciamos
				if (cuantosManifestantes == 0)
					tiempoSolo += Time.deltaTime;
				else
					tiempoSolo = 0;

				/*******************************************************
				// CONDICIONES PARA CONVERTIR UN PEATON EN MANIFESTANTE
				********************************************************/
				if (apoyo > (25 + (salario / 500) - ((65 - edad) / 10)) && energia > 10 && valor > 0 && !estaOcupado) {
					esPeaton = false;
					esManifestante = true;				
					//BUscamos al lider y le ponemos el mismo destino
					try {
						comportamientoH.destino = GameObject.Find("Lider Alpha").GetComponent<ComportamientoHumano>().destino;

					}catch{}
					//Actualizamos los contadores e manifestantes y peatones, del manager
					Manager.temp.AddManifest();
					Manager.temp.LessPeatones();
					Manager.temp.unidades.Add (this.gameObject);	
					Manager.temp.IncConciencia(10);
					//Le damos un grado de apoyo, valor y activismo minimos
					if (valor < 0) valor += cuantosManifestantes + cuantosCantando;
					if (apoyo < Manager.temp.activismoActivista) apoyo = Manager.temp.activismoActivista + cuantosManifestantes + cuantasPancartas;
					activismo += cuantosManifestantes;
					//Añadimos al log el suceso
					Manager.temp.sucesos.Add (name + " se unio a la manifestacion.");	
				}
			}

			//***************
			// MANIFESTANTE
			//***************
			//Modificamos el estado de cada manifestante, en funcion de lo que ocurre a su alrededor
			if (esManifestante) {				
				energia += cuantosCantando+cuantosBailando / 100 * (Time.deltaTime);
				activismo   += (cuantasPancartas + cuantosManifestantes + cuantosCantando)/100 - (cuantosAtacando / 100) * Time.deltaTime;
				valor  -= ((cuantosPolicias + cuantosAtacando/10) - ((cuantosManifestantes+cuantosCantando) / 100) -cuantosBailando)* Time.deltaTime;

				//Si el lider esta cerca, influencia en las unicades
				if (liderCerca) {
					activismo += (valor / 200f) * Time.deltaTime;
					valor += (activismo / 200f) * Time.deltaTime;
				}

				//El lider nunca deja de ser activista
				if (esLider && activismo < Manager.temp.activismoActivista) activismo = 55;
									
				//Si suena musica y hay energia, baila
				if (suenaMusica && energia > 50){ 
						if (!terceraPersona)
							estaBailando = true;
						else
							comportamientoH.suenaMusica = true;
				}
				else { 
					if (estaBailando || comportamientoH.suenaMusica){
						comportamientoH.suenaMusica = false;
						estaBailando = false;
					}
				}

				//Si esta solo o con poca gente, va cerca de su persona empatia, si ya esta cerca, se van con el lider. 
				if (cuantosManifestantes < 2) { 
					if (Vector3.Distance(empatiaPersona.transform.position, transform.position) > rangoEscucha)
						comportamientoH.destinoTemp = empatiaPersona.transform;
					else
						comportamientoH.destinoTemp = GameObject.Find("Lider Alpha").transform;
					//Iniciamos el movimiento por orden directa
					comportamientoH.moviendose = true;
				}

				//Si esta cantando solo o casi solo, deja de cantar
				if (estaCantando &&  tiempoCantando > cuantosCantando + (activismo/(10-cuantosCantando))) {
					estaCantando = false;
					tiempoCantando = 0f; 
				}

				//Algoritmo para, si no esta cantando, empezar a cantar, REVISO!!!
				else if (!estaCantando && cuantosCantando > 3 && (energia + activismo) > (200 / cuantosCantando)) {
					estaCantando = true;
					tiempoCantando += Time.deltaTime; 
				}

				/*********************************************
				//CONDI0CIONES PARA DEJAR DE SER MANIFESTANTE
				/********************************************/
				// SI tiene mucho miedo o esta muy cansado, se convierte en peaton.
				if ((valor < -20 && activismo < Manager.temp.activismoActivista) || (valor < -40) || (apoyo < -10) 
				    || energia <= 0) {
					esPeaton = true;
					esManifestante = false;
					try {
						//Asignamos un destino de el peaton padre 
						comportamientoH.destino = GameObject.Find("Peaton").GetComponent<ComportamientoHumano>().destino;
						//Lo ponemos en movimiento hacia alli
						isMoving(true);
					}catch{}

					//Actualizamos los contadores e manifestantes y peatones, del manager
					Manager.temp.AddPeatones();
					Manager.temp.LessManifest();
					Manager.temp.unidades.Remove  (this.gameObject);	
					Manager.temp.LessConciencia(10);

					//Si el lider Alpha deja la mani, se acaba el juego!						         
					if (name == "Lider Alpha") 
						Gui.temp.EndGameLost();

					//Añadimos al log el suceso
					Manager.temp.sucesos.Add (name + " deja de ser manifestante.");	

					if (terceraPersona)
						Gui.temp.saliendoTerceraPersona(this.gameObject);
					selectedManager.temp.deselect(this.gameObject);	
					
				}
				//***************
				//   ACTIVISTA
				//**************
				if (activismo >= Manager.temp.activismoActivista) {
					//los Activistas tambien son manifestantes, asi que esto se suma a lo anterior
					energia += (cuantosManifestantes + cuantosCantando) / 100  - (Time.deltaTime/10);
					apoyo   += (cuantosManifestantes + cuantosCantando) / 100 + (cuantosAtacando / 100);
					valor  += (cuantosPolicias / 200) + (cuantosAtacando / 100);

					//El lider nunca pierde el valor del todo
					if (esLider && valor<0) valor = 10;
				}
			}
		}//end if (hay gente alrededor)


		//**********************
		//MANIFESTANTE ATACANDO
		//**********************
		if (esManifestante && estaAtacando) {
			//Si esta atacando, pero no tiene un objetivo
			if (!objetivoInteractuar) 
				//Le buscamos uno
				objetivoInteractuar = buscarObjetivo();	
			else {
				// Si el objetivo esta fuera dle rango de escucha, nos movemos hacia el
				if (Vector3.Distance(this.transform.position, objetivoInteractuar.position) > rangoEscucha) {
					moverseParaAtacar();
				}
				//Si el objetivo esta a tiro, iniciamos el ataque
				else {
					//Si tiene objto arrojadizo y no esta arrojando, iniciamos ataque a distancia
					if (tieneOVNI && !arrojandoObjeto) {
						//El manifestante mira hacia el objetivo
						transform.LookAt(objetivoInteractuar.position);
						//Indicamos que accion/animacion, desde la cual se llama a accionArrojar();
						GetComponent<ComportamientoHumano>().iniciarAccion("Arrojar");
						//accionArrojar();
					}
					//SI no tiene objeto arrojadizo, nos movemos hacia el objetivo para atacar cuerpo a cuerpo
					else {
						moverseParaAtacar();
					}
				}
			}
		}

		//*******************************
		// SELECCION MULTIPLE DE UNIDADES
		//*******************************
		//Si desde el GUI estamos arrastrando el raton, vemos si nuestro manifestante esta dentro de ese arrastre
		if (esManifestante && Gui.temp.mouseDrag)
		{
			//Posiciones del cuadro de arrastre
			Vector3[] dragPos = Gui.temp.dragLocations();

			//Posicion del manifestante actual en pantalla.
			Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);

			//Si esta dentro, lo marcamos como seleccionado.
			if (screenPos.x > dragPos[0].x && screenPos.x < dragPos[1].x && screenPos.y > dragPos[0].y && screenPos.y < dragPos[1].y)
			{
				if (!GetComponent<selected>().isSelected)
				{
					Gui.temp.seleccionarUnidad (this.gameObject);
				}
			}
			else
			{
				if (GetComponent<selected>().isSelected)
				{
					Gui.temp.desseleccionarUnidad (this.gameObject);
				}
			}
		}	
		if (energia > 100) energia = 100;
		if (valor > 50) valor = 50;
		if (activismo > 100) activismo = 100;
		if (enfado > 100) enfado = 100;
		if (apoyo > 100) apoyo = 100;
	}//Fin del Update

	//Atacando
	public void moverseParaAtacar() {

		//Indicamos el objetivo a atacar e iniciamos el movimiento por orden directa
		GetComponent<ComportamientoHumano>().destinoTemp = objetivoInteractuar;
		GetComponent<ComportamientoHumano>().moviendose = true;
		//marcador para dibujar lineas de ataque
		moveToAttack = true;
		estaAtacando = true;
		//Si esta parado iniciara el movimiento
		isMoving(true);
	}

	//Buscamos el objeto atacable mas cercano, teniendo en cuenta que se relleno el ObjetctInRange previamente
	public Transform buscarObjetivo(){
		Transform objetoCercano = null;

		//Posibles objetivos alrededor
		objectsInRange = Physics.OverlapSphere(transform.position, rangoEscucha);

		//Hacemos un analisis de los objetos a nuestro alrededor.
		foreach (Collider objeto in objectsInRange)  {

			//Si hay algun objeto atacable, lo marcamos como objetivo
			if (objeto.tag == "Contenedores" || objeto.tag == "Policias" || objeto.tag == "Destruible"
			    || objeto.tag == "Coches"|| objeto.tag == "Destruibles") {

				//Buscamos el objeto atacable mas cercano
				if (!objetoCercano) 
					objetoCercano = objeto.gameObject.transform;
				else if (Vector3.Distance(this.transform.position,objeto.transform.position) 
				         < Vector3.Distance(this.transform.position,objetoCercano.position))
						objetoCercano = objeto.gameObject.transform;
			}
		}
		return objetoCercano;
	}

	//Detenemos las acciones que se estan llevando a cabo
	public void stopAcciones()
	{
		estaBailando = false;
		estaCantando = false;
		estaReproduciendoMusica = false;
		estaEnActoPoetico = false;
	}

	//Detenemos el ataque
	public void stopAttack()
	{
		moveToAttack = false;
		estaAtacando = false;
		GetComponent<Animator> ().SetBool ("AtaqueCorto",false);
	}

	//Iniciamos o detenemos el movimiento de una unidad
	public void isMoving(bool m)
	{
		//desactivamos/activamos el movimiento, si no esta KO
		if (!estaKO) {
			estaParado = !m;
			tiempoCorriendo = 0;
			tiempoParado += Time.deltaTime;
		}
		else {
			estaParado = true;
		}
	}

	//Detenemos el lanzamiento de objetos
	public void stopArrojar()
	{
		if (arrojandoObjeto) {
			arrojandoObjeto = false;
			GetComponent<ComportamientoHumano>().cambioCamara(false);
			//Correccion de un giro que da cuando arroja una piedra
			transform.Rotate(Vector3.up,80);
		}

	}

	//Iniciamos el lanzamiento de objetos
	public void accionArrojar()
	{

		//Descubrimos en que mano tiene el objeto y lo arrojamos 
		if (manoIzquierda.GetComponent<ObjetoDeMano>().esArrojable) {
			//Arrojamos una copia del objeto arrojable de la mano izquierda
			Arrojar(manoIzquierda.GetComponent<ObjetoDeMano>().objetoArrojar);
			//Objetos infinitos. Descomentar para objeto unico
			//manoIzquierda = manoVacia;
		}
		else if (manoDerecha.GetComponent<ObjetoDeMano>().esArrojable) {		
			//Arrojamos una copia del objeto arrojable de la mano izquierda
			Arrojar(manoDerecha.GetComponent<ObjetoDeMano>().objetoArrojar);
			//Objetos infinitos. Descomentar para objeto unico
			//manoDerecha = manoVacia;
		}
		
		//SI ES MUY ACTIVISTA, TIENE MAS OBJETOS EN LA MOCHILA?? (Desafio: objetos en la mochila)
	}

	//Accion de arrojar un objeto
	public void Arrojar (GameObject objetoArrojar) {

		//Correccion del eje de rotacion modificado por la animacion
		//transform.Rotate(Vector3.up,40);
		//Generamos una copia del objeto fisico generico que vamos a arrojar. 
		GameObject objetoArrojado = (GameObject)UnityEngine.Object.Instantiate(objetoArrojar, 
		                                                                       transform.position + transform.forward + transform.up*3, 
		                                                                       transform.rotation);

		//Calculamos el vector direccion de lanzamiento
		Vector3 forceDirection = Camera.main.transform.forward*fuerza + Vector3.up*fuerza;

		//Añadimos la fuerza de lanzamiento al objeto clonado
		objetoArrojado.GetComponent<Rigidbody>().AddForce(forceDirection, ForceMode.Impulse);

		//Asignamos las varaiables de estado del objeto arrojado
		objetoArrojado.GetComponent<ObjetoDeMano>().enElSuelo = false;
		objetoArrojado.GetComponent<ObjetoDeMano>().enVuelo = true;

		
	}

}
