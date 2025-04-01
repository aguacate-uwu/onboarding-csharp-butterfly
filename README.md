
# 📚 Despliegue de programa C# con Docker Compose
Este proyecto utiliza Docker y Docker Compose para desplegar un script de C# y una base de datos PostgreSQL de manera rápida y sencilla.

---

## 🛠️ Requisitos Previos
Antes de comenzar, asegúrate de tener instalados en tu sistema:

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)
---

## 🚀 Instalación y Puesta en Marcha

### 1️⃣ Clonar el repositorio
Ejecuta el siguiente comando para clonar el proyecto:
```bash
git clone git@github.com:campus-CodeArts/onboarding-csharp-extra.git
cd onboarding-csharp-extra
```

### 2️⃣ Levantar los contenedores
Para iniciar los servicios en segundo plano, ejecuta:
```bash
docker compose up -d
```
📌 **Nota:** La primera vez que inicies los servicios, puede tardar un rato.

### 3️⃣ Verificar que los contenedores están corriendo
Comprueba el estado del contenedor con:
```bash
docker ps
```
Deberías ver un contenedor en ejecución: **postgres-db**.

### 4️⃣ Instalación de .NET 9
Ejecuta los siguiente comandos en orden en tu terminal:
```bash
sudo add-apt-repository ppa:dotnet/backports
```

```bash
sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-9.0
```

Comprueba que se ha instalado correctamente ejecutando el siguiente comando para ver la versión de .NET

```bash
dotnet --version
```

### 5️⃣ Ejecutar el programa
Para inciar el programa, ejecuta desde la carpeta raíz este comando:
```bash
dotnet run
```

Si no funciona, asegurate de tener levantado y funcionando correctamente los contenedores

## 🔄 Detener y Reiniciar los Contenedores
Si deseas detener los contenedores en ejecución:
```bash
docker compose down
```
Para volver a iniciarlos:
```bash
docker compose up -d
```

---

## 🧹 Eliminar los Contenedores y Datos Persistentes
Si quieres eliminar los contenedores junto con los volúmenes y datos almacenados:
```bash
docker compose down -v
```
⚠️ **Advertencia:** Esto eliminará todos los datos almacenados en la base de datos PostgreSQL.

---

## 🎯 Notas Finales
- Para ver los registros en tiempo real:
  ```bash
  docker compose logs -f
  ```

Para más información sobre **C#** consulta su documentación oficiales.
