# KinoJoin

Follow the instructions below to setup the project correctly.

### Postgres Connection setup
Go to presentation/presentation and run the command: <br>
```dotnet user-secrets init```

Then run this command  ({YourPassword} should be replaced with your Postgres password): <br>
```dotnet user-secrets set "PostgresConnection" "Host=localhost;Database=KinoJoin;Username=postgres;Password={YourPassword};Port=5432;Include Error Detail=true;"```

### Tailwind
To recompile the tailwind css file, run the command below fro mthe root of the solution: <br>
```tailwindcss -i .\Presentation\Presentation\wwwroot\app.css -o .\Presentation\Presentation\wwwroot\app.min.css```

This needs to be done everytime you have used a new tailwind class. Therefore it is a good idea to use a system that does this automatically.
This can be done by running the below command in a terminal that you keep open: <br>
``` tailwindcss -i .\Presentation\Presentation\wwwroot\app.css -o .\Presentation\Presentation\wwwroot\app.min.css -w```

It can also be done by setting up a launch configuration that runs the command automatically, everytime you start the project.


### Appsettings.json
You need to replace the code in the file ```KinoPrototype.client/wwwroot/appsettings.json``` with the code below: <br>
```json
 {
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },

  "Auth0": {
    "Authority": "YourAuth0Domain",
    "ClientId": "YourAuth0ClientId"
  },
  
  "HostOnNgrokWithNoHttpsAndSetDefaultUser": true
}
```

### Authentication
We are using Auth0 for authentication. You need to create an Auth0 application and replace "YourAuth0Domain" and "YourAuth0ClientId" in Appsettings.json with the domain and client id of your Auth0 application.
You can find this on the Auth0 dashboard under
Applications -> applications -> your application -> settings ->
"Domain" and "Client" ID

Remember to scroll to Application URIs, and set callback urls, or alternatively follow Auth0 own guide, which is great.


### Self hosting
We use  a combination of nginx and ngrok for self hosting. 

#### Nginx
In order to use nginx first you need to download it from here: https://nginx.org/en/download.html.
You then need to go to the folder where nginx is stored on you comuter and find the file conf/nginx.conf. Replace the code in this file with the code below:
```nginx
worker_processes  1;

events {
    worker_connections  1024;
}

http {
    include       mime.types;
    default_type  application/octet-stream;

    sendfile        on;

    server {
        listen       80;
        server_name  localhost;

        location ~ /.well-known/acme-challenge {
            alias C:/nginx; # Use the directory where NGINX is installed
            allow all;
        }

        location / {
            proxy_pass http://localhost:5000; # HTTP on port 5000
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection keep-alive;
            proxy_set_header Host $host;
            proxy_cache_bypass $http_upgrade;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        error_page   500 502 503 504  /50x.html;
        location = /50x.html {
            root   html;
        }
    }

}
```

On the line ```proxy_pass http://localhost:5000;``` replace 5000 with the port that the project is running on, if it is different from 5000. 
5000 is the default port.

#### Ngrok

To use ngrok download it from here: https://ngrok.com/download.

To host the project on ngrok you need to run the project in http, then run the command below in any terminal: <br>
```ngrok http 80```

The variable "HostOnNgrokWithNoHttpsAndSetDefaultUser" in appsettings.json should be set to true if you are hosting the project on ngrok.
This is because https and authentication does not work on ngrok. Instead a default user will be used for creating events. In all other circumstances, the variable should be set to false.

### Testing
In order to run tests you must have Docker Desktop open.