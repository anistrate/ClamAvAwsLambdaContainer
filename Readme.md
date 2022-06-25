# ClamAv Aws Lambda Container
An AWS Lambda project which scans files uploaded to an S3 bucket, using the free Clam antivirus as a Lambda container.

## Technologies
* Net Core 5.0
* C#
* AWS Lambda
* AWS S3
* AWS Elastic Container Registry (ECR)
* [nClam library](https://github.com/tekmaven/nClam)
* Docker

## Motivation
Many websites offer a file upload functionality but few scan the files to see if they're infected. This project offers an easy solution for files uploaded to an S3 bucket.

## How it works

Clam AntiVirus is a free open source (GPLv2) anti-virus toolkit. It can be set up to run as a server, allowing clients to connect to it and scan files for viruses.
AWS Lambda functions can be packaged as container images, allowing the function to interact with the underlying OS and its resources. The Docker file installs and configures the Clam Av server during the build phase, and the C# code starts the Clam server and interacts with it at runtime. 

## Typical workflow

1. A file is uploaded to an S3 bucket.
2. An S3 Event is generated.
3. The lambda function is triggered, creating the container.
4. The C# code in the main function starts the ClamAV process, waits for it to accept connections on port 3310, and then scans the file which triggered the lambda.
5. The results of the scan is logged using Aws Cloudwatch.

## How to use 

The functionality presented here is minimal, in a real world scenario you would want to do more things, such as send emails with negative scan results, move infected files to a "quarantine" folder etc. You should clone this repository and modify it to suit your needs.

## Setup

1. Clone the repository locally.
2. Test the project locally, building it with Docker, like any normal project. More  on how to simulate an S3Event locally [here](https://docs.aws.amazon.com/lambda/latest/dg/images-test.html)
3. Push it to an Elastic Container Registry repositry.
4. Create a Lambda function and select the repository created at 3 as the image.
5. Create a trigger which will call the function on file upload in the bucket and folder of your choosing.
6. Ensure that the Lambda function has the necessary permissions to access CloudWatch.
7. The Lambda function uses roles to get all the necessary permissions to access the S3 in order to scan the file. Ensures it has enough permissions to read the file trigerring the event. 

## Additional Configurations

1. The ECR repository needs at least 500 MB of space, most of it for the virus definition database.
2. The Lambda function needs at least 2048 MB of storage. When first invoked the function will first load the virus definition database in memory so the total memory used will be 1500 mb + the size of the file(s) being scanned.
3. This project needs the EPEL repository in order to install Clam AV. Currently, the package is [here](https://dl.fedoraproject.org/pub/epel/epel-release-latest-7.noarch.rpm). If this link were to become unavailable, the solution will stop working and a different source will have to be found.
5. The maximum size of a file to be scanned is 100MB. If you need to change this simply modify `100M` from following line in docker file:
`RUN sed -i 's/\#StreamMaxLength 10M/StreamMaxLength 100M /g' /etc/clamd.d/scan.conf`

## FAQ

* How long does it take to scan a file?

It depends whether it's a "cold" or "hot" start, [see here](https://aws.amazon.com/blogs/compute/operating-lambda-performance-optimization-part-1/). When the function is first invoked it will take between 30s to 1 min 20s to start, with an average of 50 seconds. If the function is called again after it's been loaded into memory, the call takes 3-10 seconds, depending on the size of the file.
If you need the function to take as little as possible in order to not impact the user experience, consider warming up the function before the user uploads a file.

* How much memory does the fuction use?

1500 MB for the OS plus loading the virus definition into memory, plus the size of the files to be scanned. Calling the function 5 times for 5 files each 100 MB in size will consume 2000 MB of memory.

* Does Clam Av need to be started from the C# code? Can't it be started by the OS at runtime?

Unfortunately no, at least not anymore(*). Containers have only one point of entry and Lambda Containers are required by AWS to set it to the name of the function. I've tried to navigate around this but Docker is proactively antagonistic to users trying to install `systemd`. Neither `cron` nor `rc.local` are available, so manually starting the Clam server is the only option. 

 (*) At one point, while tinkering in the internals of the AWS image I managed to find the script the entrypoint was using behind the scenes to run things. I managed to force it to start clam server by hacking it at the build phase using the line
 `RUN sed -i '/USER_LAMBDA_BINARIES_DIR=/a  /usr/sbin/clamd' /lambda-entrypoint.sh`
This forced the contaier to start the clamd process and to also ensure it's properly terminated, as part of the shutdown. Unfortunately, starting with January 2022 this does not work anymore. It seems AWS adds the `lambda-entrypoint.sh` file after the build has finished, preventing the user from modifying with it.

* What does the line `Run sed -i 's/\#LocalSocket \/run\/clamd.scan\/clamd.sock/LocalSocket \/tmp\/clamd.sock/g' /etc/clamd.d/scan.conf` do?

The folder where clamd listens for connections is not available to the AWS user, which only has read and write access in the tmp folder. This line instructs clamd to use the tmp folder to open the `clamd.sock` file in order to accept connections.

## License

This project is licensed under the [MIT License](https://github.com/anistrate/ClamAvAwsLambdaContainer/blob/main/LICENSE).
