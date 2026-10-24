# Products API — AWS Lambda CRUD in C# with SQL Server

A small learning project that shows how to build a REST API on AWS using C#, Lambda and SQL Server.

## 1. What this project does

It lets you **C**reate, **R**ead, **U**pdate and **D**elete products through a web API.

| Method | URL              | What it does                             | Success code |
|--------|------------------|------------------------------------------|--------------|
| POST   | `/products`      | Create a product                         | 201          |
| GET    | `/products`      | List all products                        | 200          |
| GET    | `/products/{id}` | Get one product (404 if it doesn't exist) | 200          |
| PUT    | `/products/{id}` | Update a product (404 if it doesn't exist) | 200        |
| DELETE | `/products/{id}` | Delete a product                         | 204          |

If `name` is empty (or longer than 200 characters), or `price` is 0 or less, you get
**400 Bad Request** with a message.

A product looks like this:

```json
{
  "id": "3f2b8c1e-0b5a-4d0e-9a7e-2f1c6d9b8a11",
  "name": "Coffee Mug",
  "price": 9.99,
  "createdAt": "2026-10-24T10:15:30.123Z"
}
```

### How it fits together

```
You (curl) ──► API Gateway (HTTP API) ──► Lambda function (C#) ──► SQL Server (your existing database)
```

- **API Gateway** gives you a public URL and sends each request to the right Lambda function.
- **Lambda** runs the C# code. There are 5 functions, one for each endpoint.
- **SQL Server** is the database where products are saved, in a table called `Products`.
  The code talks to it through **Entity Framework Core (EF Core)**, which turns C# calls into SQL.

### The files

| File | What it is |
|------|------------|
| `ProductsApi.sln` | The solution file. Double-click it to open the project in Visual Studio. |
| `ProductsApi/Models/Product.cs` | The `Product` class. Its attributes describe the table columns. |
| `ProductsApi/Data/ProductsDbContext.cs` | The EF Core "database context": the connection to SQL Server. |
| `ProductsApi/Repositories/IProductRepository.cs` | The list of database operations. |
| `ProductsApi/Repositories/ProductRepository.cs` | The code that reads and writes products with EF Core. |
| `ProductsApi/Functions.cs` | The 5 Lambda functions (one per endpoint) plus validation. |
| `ProductsApi/Startup.cs` | Dependency injection: sets up EF Core with the connection string, and the repository. |
| `ProductsApi/serverless.template` | The AWS resources to create (functions, API) and the database settings. |
| `ProductsApi/aws-lambda-tools-defaults.json` | Default settings for the deploy command (region, stack name). |
| `ProductsApi/ProductsApi.csproj` | The project file with the NuGet packages. |
| `database/create-table.sql` | SQL script that creates the `Products` table. |
| `events/*.json` | Test events for calling each Lambda function directly (see section 4). |
| `nuget.config` | Makes the project download packages only from nuget.org. |

> **How the connection string gets to the code:** when you deploy, you give 4 values (server,
> database, user, password). `serverless.template` joins them into a connection string and stores
> it in the `SQL_CONNECTION_STRING` environment variable of each Lambda function.
> `Startup.cs` reads that variable.

> **Note about `serverless.template`:** The Lambda Annotations library **rewrites the function
> sections of this file every time you build**, based on the `[LambdaFunction]` and `[HttpApi]`
> attributes in `Functions.cs`. So if you want to change a URL or memory size, change the
> attribute in C# and not the template. The `Parameters`, `Globals` and `Outputs` sections are yours to edit.

## 2. Prerequisites

1. **An AWS account.** Sign up at <https://aws.amazon.com/>.
2. **AWS CLI**, installed and logged in.
   - Install: <https://aws.amazon.com/cli/>
   - Then run `aws configure` and enter your Access Key, Secret Key and region (for example `us-east-1`).
   - Check that it works: `aws sts get-caller-identity`
3. **.NET 8 SDK.** Install from <https://dotnet.microsoft.com/download/dotnet/8.0>.
   - Check: `dotnet --list-sdks` should show a line starting with `8.`
4. **Amazon.Lambda.Tools** (adds `dotnet lambda` commands):
   ```bash
   dotnet tool install -g Amazon.Lambda.Tools
   ```
   If it's already installed, update it with `dotnet tool update -g Amazon.Lambda.Tools`.
5. **A SQL Server that AWS Lambda can reach**, for example Amazon RDS for SQL Server.
   - You need a **SQL login** (user name + password). Windows logins don't work from Lambda.
   - Lambda runs in AWS, so it **cannot** reach a SQL Server running only on your own laptop
     (like LocalDB or SQL Server Express on `localhost`).
   - If your server is not reachable from the internet (a private RDS instance), see
     [If your SQL Server is private](#if-your-sql-server-is-private-inside-a-vpc) below.
6. **A tool to run SQL**, such as SQL Server Management Studio (SSMS), Azure Data Studio or `sqlcmd`.

## 3. Deploy step by step

**Step 1 — Create the database and table.** Connect to your SQL Server and run
[`database/create-table.sql`](database/create-table.sql). It creates the `ProductsDb` database
(if it doesn't exist), switches to it, and creates the `Products` table.
With `sqlcmd` it looks like this:

```bash
sqlcmd -S myserver,1433 -U myuser -P "mypassword" -C -i database/create-table.sql
```

The script skips anything that already exists, so running it twice is safe.
If you want a different database name, change `ProductsDb` in the script and use the same name for `SqlDatabase` when you deploy.

**Step 2 — Go to the project folder.**

```bash
cd ProductsApi/ProductsApi
```

**Step 3 — Build once to make sure everything compiles.**

```bash
dotnet build
```

**Step 4 — Create an S3 bucket.** The deploy command uploads your compiled code to S3 first.
Bucket names must be unique across all of AWS, so add something personal (like your name and a number):

```bash
aws s3 mb s3://products-api-deploy-yourname-123 --region us-east-1
```

**Step 5 — Deploy.** Replace the 4 database values with your own:

```bash
dotnet lambda deploy-serverless --s3-bucket products-api-deploy-yourname-123 \
  --template-parameters "SqlServer=mydb.abc123.us-east-1.rds.amazonaws.com,1433;SqlDatabase=ProductsDb;SqlUser=myuser;SqlPassword=mypassword"
```

(In PowerShell, put the whole command on one line or replace `\` with a backtick `` ` ``.)

| Parameter | What to put there |
|-----------|-------------------|
| `SqlServer` | Server address, a comma, and the port (normally `1433`). For RDS this is the "Endpoint" shown in the RDS console. |
| `SqlDatabase` | The database where you created the `Products` table. Default: `ProductsDb`. |
| `SqlUser` | Your SQL login name. |
| `SqlPassword` | Your SQL login password. It is hidden in the AWS console. Don't use `;` in it, because `;` separates the values in this command. |

This command:
1. Compiles the code for Linux and zips it.
2. Uploads the zip and the template to your S3 bucket.
3. Creates a **CloudFormation stack** called `products-api` (the name comes from
   `aws-lambda-tools-defaults.json`). The stack creates the 5 Lambda functions, their permissions
   and the API Gateway. It does **not** create the database: it uses yours.

It takes 1–3 minutes. At the end you'll see an **Outputs** section like this:

```
Output Name                    Value
------------------------------ --------------------------------------------------
ApiUrl                         https://abc123xyz.execute-api.us-east-1.amazonaws.com
```

**Copy the `ApiUrl`.** You'll use it in the next section.

> To use a different region, change `region` in `aws-lambda-tools-defaults.json`
> (and create the S3 bucket in that same region).

### If your SQL Server is private (inside a VPC)

If your SQL Server is a private RDS instance, Lambda must run inside the same VPC to reach it.
Add a `VpcConfig` to the `Globals` → `Function` section of `serverless.template`, next to `Environment`:

```json
"VpcConfig": {
  "SecurityGroupIds": [ "sg-0123456789abcdef0" ],
  "SubnetIds": [ "subnet-0aaa1111bbbb2222c", "subnet-0ddd3333eeee4444f" ]
}
```

- `SubnetIds`: subnets in the same VPC as your database (find them in the **VPC** console).
- `SecurityGroupIds`: a security group for the Lambda functions.
- The **database's** security group must allow inbound traffic on port **1433** from that Lambda security group.

The functions already have the `AWSLambdaVPCAccessExecutionRole` permission that this needs.

## 4. Test each endpoint

First, save your API URL in a variable (use your own URL from the deploy output).

**macOS / Linux / Git Bash:**

```bash
API=https://abc123xyz.execute-api.us-east-1.amazonaws.com
```

### Quick reference: all curl commands

Copy-paste list. The sections below explain each command and show the expected result.

**macOS / Linux / Git Bash:**

```bash
API=https://abc123xyz.execute-api.us-east-1.amazonaws.com
ID=paste-a-product-id-here

# Create (201)
curl -i -X POST "$API/products" -H "Content-Type: application/json" -d '{"name": "Coffee Mug", "price": 9.99}'

# Get all (200)
curl -i "$API/products"

# Get one (200, or 404 if not found)
curl -i "$API/products/$ID"

# Update (200, or 404 if not found)
curl -i -X PUT "$API/products/$ID" -H "Content-Type: application/json" -d '{"name": "Big Coffee Mug", "price": 12.50}'

# Delete (204)
curl -i -X DELETE "$API/products/$ID"

# Validation errors (400)
curl -i -X POST "$API/products" -H "Content-Type: application/json" -d '{"name": "", "price": 5}'
curl -i -X POST "$API/products" -H "Content-Type: application/json" -d '{"name": "Pen", "price": 0}'
```

**Windows Command Prompt (cmd.exe):** JSON quotes must be escaped with `\"`.

```bat
set API=https://abc123xyz.execute-api.us-east-1.amazonaws.com
set ID=paste-a-product-id-here

curl -i -X POST "%API%/products" -H "Content-Type: application/json" -d "{\"name\": \"Coffee Mug\", \"price\": 9.99}"
curl -i "%API%/products"
curl -i "%API%/products/%ID%"
curl -i -X PUT "%API%/products/%ID%" -H "Content-Type: application/json" -d "{\"name\": \"Big Coffee Mug\", \"price\": 12.50}"
curl -i -X DELETE "%API%/products/%ID%"
```

In PowerShell, use the `Invoke-RestMethod` commands further down instead of curl.

### Create a product (POST)

```bash
curl -i -X POST "$API/products" \
  -H "Content-Type: application/json" \
  -d '{"name": "Coffee Mug", "price": 9.99}'
```

Expected: `HTTP/1.1 201 Created` and the new product with its `id`. **Copy the `id`** for the next steps.

```bash
ID=paste-the-id-here
```

### Get all products (GET)

```bash
curl -i "$API/products"
```

Expected: `200 OK` and a JSON list of products.

### Get one product (GET)

```bash
curl -i "$API/products/$ID"
```

Expected: `200 OK` and the product. Try an id that doesn't exist to see a `404`:

```bash
curl -i "$API/products/does-not-exist"
```

### Update a product (PUT)

```bash
curl -i -X PUT "$API/products/$ID" \
  -H "Content-Type: application/json" \
  -d '{"name": "Big Coffee Mug", "price": 12.50}'
```

Expected: `200 OK` and the updated product. The `id` and `createdAt` stay the same.

### Delete a product (DELETE)

```bash
curl -i -X DELETE "$API/products/$ID"
```

Expected: `204 No Content`. Run "Get one product" again and you'll get a `404`.

### See the validation errors (400)

```bash
curl -i -X POST "$API/products" -H "Content-Type: application/json" -d '{"name": "", "price": 5}'
# -> 400 {"message":"Name is required."}

curl -i -X POST "$API/products" -H "Content-Type: application/json" -d '{"name": "Pen", "price": 0}'
# -> 400 {"message":"Price must be greater than 0."}
```

### Check the data in SQL Server

You can see what the API saved by running this in SSMS or `sqlcmd`:

```sql
SELECT * FROM dbo.Products;
```

### Using Windows PowerShell instead

Quoting JSON for `curl` in PowerShell is awkward, so use `Invoke-RestMethod`:

```powershell
$API = "https://abc123xyz.execute-api.us-east-1.amazonaws.com"

# Create
$product = Invoke-RestMethod -Method Post -Uri "$API/products" -ContentType "application/json" -Body '{"name": "Coffee Mug", "price": 9.99}'
$ID = $product.id

# Read all / read one
Invoke-RestMethod -Uri "$API/products"
Invoke-RestMethod -Uri "$API/products/$ID"

# Update
Invoke-RestMethod -Method Put -Uri "$API/products/$ID" -ContentType "application/json" -Body '{"name": "Big Coffee Mug", "price": 12.50}'

# Delete
Invoke-RestMethod -Method Delete -Uri "$API/products/$ID"
```

### Call a Lambda function directly with the event files

The `events/` folder has one test event per function. Each file contains the JSON that API Gateway
sends to Lambda, so you can run a function **without going through the API URL**.

| Event file | Lambda function (resource name in `serverless.template`) | Same as |
|------------|------------------------------------------------------------|---------|
| `events/create-event.json`  | `CreateProductFunction` | `POST /products` |
| `events/get-all-event.json` | `GetProductsFunction`   | `GET /products` |
| `events/get-event.json`     | `GetProductFunction`    | `GET /products/{id}` |
| `events/update-event.json`  | `UpdateProductFunction` | `PUT /products/{id}` |
| `events/delete-event.json`  | `DeleteProductFunction` | `DELETE /products/{id}` |

The most important parts of an event are:
- `pathParameters.id`: the `{id}` in the URL. In `get-event.json`, `update-event.json` and
  `delete-event.json`, replace every `REPLACE-WITH-PRODUCT-ID` with a real product id.
- `body`: the request JSON, written as **a string** (that's why the inner quotes are escaped as `\"`).

**Step 1 — Find the real function name.** In AWS, the functions get longer names such as
`products-api-CreateProductFunction-AbC123`. Look one up like this:

```bash
aws cloudformation describe-stack-resource --stack-name products-api --logical-resource-id CreateProductFunction --query StackResourceDetail.PhysicalResourceId --output text
```

(Change `CreateProductFunction` to the function you want.)

**Step 2 — Invoke it with an event file** (run from the top `ProductsApi` folder):

```bash
aws lambda invoke --function-name <name-from-step-1> --cli-binary-format raw-in-base64-out --payload file://events/create-event.json response.json
```

**Step 3 — Read the result** in `response.json`. It contains `statusCode` (for example `201`)
and the product in `body`.

You can also paste the content of an event file into the **Test** tab of a function in the AWS Lambda console.

### Seeing logs

Each function writes logs to **CloudWatch**. In the AWS Console, go to **Lambda → pick a function → Monitor → View CloudWatch logs**.

If every request returns **500** or times out after 30 seconds, the function usually can't connect
to SQL Server. Check the logs for a `SqlException`, then check the server address, user, password and
network access (see [If your SQL Server is private](#if-your-sql-server-is-private-inside-a-vpc)).

## 5. Delete everything from AWS after testing

**Step 1 — Delete the stack.** This removes the Lambda functions, the API Gateway and the IAM roles:

```bash
dotnet lambda delete-serverless products-api
```

**Step 2 — Delete the S3 bucket** you created for deployment (`--force` also deletes the files in it):

```bash
aws s3 rb s3://products-api-deploy-yourname-123 --force
```

**Step 3 (optional) — Delete the log groups.** CloudWatch log groups are not deleted with the stack.
List them, then delete each one:

```bash
aws logs describe-log-groups --log-group-name-prefix /aws/lambda/products-api --query "logGroups[].logGroupName"
aws logs delete-log-group --log-group-name /aws/lambda/<name-from-the-list>
```

**Step 4 — Check.** In the AWS Console, open **CloudFormation**. The `products-api` stack should be gone.

**Your SQL Server is not touched** by these steps, because it wasn't created by the stack. If you
want to remove the test data, run this in your database:

```sql
DROP TABLE dbo.Products;
```

## Things to know (for later)

- **Password in an environment variable:** to keep things simple, the connection string (with the
  password) is stored as a Lambda environment variable. Anyone who can view the function in the AWS
  console can see it. In a real project you'd keep the password in **AWS Secrets Manager**.
- **`TrustServerCertificate=True`** in the connection string means the connection is encrypted, but
  the server's certificate isn't checked. That's common for learning setups. In production, install
  the RDS certificate and remove this setting.
- **GET /products returns every row.** That's fine here, but a real API would return results page by page.
- **No authentication.** Anyone with the URL can call the API, so delete the stack when you're done.
- **Private NuGet feeds:** `nuget.config` makes this project use only nuget.org. Without it,
  a company package feed in your global NuGet settings can make `dotnet build` fail with `401 Unauthorized`.
