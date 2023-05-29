FROM public.ecr.aws/lambda/dotnet:6 AS base
EXPOSE 3310

#have your linux up to date
RUN yum update -y

#we have to install the epel repository in order to get Clam Av. As for now, this is the only way to do it. If the link were to stop working a different solution should be sought out.
RUN yum install -y https://dl.fedoraproject.org/pub/epel/epel-release-latest-7.noarch.rpm

#actuall clam components needed to run the antivirus
RUN yum install -y clamav clamav-server clamav-update clamav-lib

#utilities for local debugging purposes, uncomment when you want to run the container locally and debug
#RUN yum install -y iproute procps telnet net-tools nano 

#configuring the clamd service, basically modifying config files
Run sed -i 's/\#LocalSocket \/run\/clamd.scan\/clamd.sock/LocalSocket \/tmp\/clamd.sock/g' /etc/clamd.d/scan.conf
RUN sed -i 's/\#TCPSocket /TCPSocket /g' /etc/clamd.d/scan.conf
RUN sed -i 's/\#TCPAddr /TCPAddr /g' /etc/clamd.d/scan.conf
RUN sed -i 's/\#StreamMaxLength 10M/StreamMaxLength 100M /g' /etc/clamd.d/scan.conf
RUN sed -i 's/\#MaxFileSize 30M/MaxFileSize 100M /g' /etc/clamd.d/scan.conf
RUN sed -i '/\#Example/d' /etc/freshclam.conf

#the freschclam service downloads the virus signature definitions
RUN freshclam

#building the project in .Net
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build

WORKDIR /src

COPY *.csproj .
RUN dotnet restore "ClamAvAwsLambdaContainer.csproj"

COPY . .
RUN dotnet publish --no-restore -c Release -o /app/publish #--no-cache

FROM base AS final
WORKDIR /var/task
COPY --from=build /app/publish .

#hack to inject a bit of code which starts clamd before lambda invocation; don't do this at home
#Commented this line as it doesn't seem to work anymore
#RUN sed -i '/USER_LAMBDA_BINARIES_DIR=/a  /usr/sbin/clamd' /lambda-entrypoint.sh

CMD ["ClamAvAwsLambdaContainer::ClamAvAwsLambdaContainer.Function::FunctionHandler"]