# BanchoNET

A dotnet implementation of [osu!](https://osu.ppy.sh)bancho server.

## Technologies

- ASP.NET Core (.NET 8)
- MYSQL (MariaDB)
- MongoDB
- Redis
- Docker (soon™)

## Progress

Go to the [Projects](https://github.com/orgs/NovemoG/projects/1) section to see the progress of the BanchoNET project.

## Features
- Player registration (currently only in-game)
- Online users listing and statuses
- Multiplayer lobbies
- Global/Country ranking leaderboards
- In-game leaderboards
- Spectating
- osu!Direct
- Server commands

## In development
- Clubs (clans)
- Beatmap submission
- Custom commands
- Website

## Building

### Prerequisites

* Dotnet SDK 8.0
* MariaDB Server
* MongoDB Server
* Nginx Server

### Building

To build the BanchoNET server, follow these steps:

1. Clone the repository to your local machine.
```bash
git clone https://github.com/NovemoG/BanchoNET.git
```
2. Navigate to the project directory.
```bash
cd BanchoNET
```
3. Restore the project dependencies
```bash
dotnet restore
``` 
4. Use the .NET build command to build the project.
```bash
dotnet build
```
This will create a build of your project in the `./bin/Debug/net8.0` directory.

5. Navigate to the nginx configuration folder
```bash
cd /etc/nginx/sites-enabled
```

6. Create banchonet configuration file
```bash
touch banchonet.conf
```

7. Edit file with following config
```conf
server {
    listen 80;
    server_name c.DOMAIN;

    location / {
        proxy_pass https://0.0.0.0:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}

server {
        listen 443 ssl;
        server_name osu.DOMAIN c4.DOMAIN c.DOMAIN;

        location / {
                proxy_pass https://0.0.0.0:5000;
                proxy_set_header Host $host;
                proxy_set_header X-Real-IP $remote_addr;
                proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        }
}
```

8. Replace `DOMAIN` with your domain.

9. Add all subdomain records that are present in the config file above.

## Usage

### Running

To run the BanchoNET server, follow these steps:

1. Navigate to the project directory.
```bash
cd BanchoNET
```
2. Use the .NET run command to run the project.
```bash
dotnet run
```
This will start the BanchoNET server. You should see output indicating that the server is running.


### Deployment

We are planning on adding docker support.

# Contributing
- **Fork** the Project
- Create your **Feature Branch** `git checkout -b feature/AmazingFeature`
- Commit your **Changes** `git commit -m 'Add some AmazingFeature'`
- Push **to the Branch** `git push origin feature/AmazingFeature`
- Open a **Pull Request**

# License

Bancho.NET is licensed under the MIT License. Please see the [LICENSE](LICENSE) file for more information.

# Acknowledgments

- Heavily inspired by [bancho.py](https://github.com/osuAkatsuki/bancho.py/)
