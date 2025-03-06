# 🚀 Instalacion de Unity en Ubuntu
Ejecuta el siguiente comando para añadir la key pública:
```bash
wget -qO - https://hub.unity3d.com/linux/keys/public | gpg --dearmor | sudo tee /usr/share/keyrings/Unity_Technologies_ApS.gpg > /dev/null
```

Añade el repositorio de Unity Hub:
```bash
sudo sh -c 'echo "deb [signed-by=/usr/share/keyrings/Unity_Technologies_ApS.gpg] https://hub.unity3d.com/linux/repos/deb stable main" > /etc/apt/sources.list.d/unityhub.list'
```

Actualiza el cache del paquete e instalalo:
```bash
sudo apt update
sudo apt-get install unityhub
```

## 🧹 Eliminar Unity Hub
Si quieres eliminar Unity Hub del sistema, lanza el siguiente comando:
```bash
sudo apt-get remove unityhub
```

## 🎯 Notas Finales
Si tienes algun problema con la instalación, comprueba lo siguiente:

- El directorio "/usr/share/keyrings" existe
- El usuario con el que estas instalando Unity Hub tiene permiso de escritura en el directorio "/usr/share/keyrings"
- El usuario con el que estas instalando Unity Hub tiene por lo menos permisos de lectura al archivo Unity_Technologies_ApS.gpg