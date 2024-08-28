using Microsoft.EntityFrameworkCore;
using lab3.DbData;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace lab3
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddHttpContextAccessor();
            services.AddControllersWithViews();
            services.AddSingleton<DynamoDBManager>();
            services.AddControllersWithViews();

            services.AddAWSService<IAmazonDynamoDB>(new AWSOptions
            {
                Region = Amazon.RegionEndpoint.USEast1 // Specify your desired region
            });


            services.AddDbContext<MovieAppDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });

            // Register Amazon S3 client
            // Register Amazon DynamoDB client with explicit credentials
            var awsCredentials = new BasicAWSCredentials("AKIA5FTZBKIPAEI4I236", "OlL9uD6WBb4vHunUigbqpkWNYhVXsJ1WJ+l5TfaO");
            services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(awsCredentials, Amazon.RegionEndpoint.USEast1));

            // Register repositories
            services.AddTransient<IMovieRepository, EFMovieRepository>();
            services.AddTransient<IUserRepository, EFUserRepository>();

            // Add SeedData as a singleton
            services.AddSingleton<SeedData>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");


            });
        }
    }
}
