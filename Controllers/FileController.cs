using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RentServer.Controllers
{
    [Route("[controller]")]
    public class FileController : BaseController
    {
        private IHostingEnvironment hostingEnv;

        public FileController(IHostingEnvironment env)
        {
            this.hostingEnv = env;
        }

        [HttpPost("upload")]
        public JsonResult UploadFile(List<IFormFile> files)
        {
            var filePathList = new List<string>();
            foreach (var formFile in files)
            {
                //得到的名字是文件在本地机器的绝对路径
                var fileName = formFile.FileName.ToString();

                string filePath = hostingEnv.WebRootPath + $@""+Path.DirectorySeparatorChar+"static"+Path.DirectorySeparatorChar+"imgs" + Path.DirectorySeparatorChar;

                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                fileName = Guid.NewGuid() + "." + fileName.Split('.')[1];

                string fileFullName = filePath + fileName;

                if (formFile.Length > 0)
                {
                    //根据路径创建文件
                    using (var fs = new FileStream(fileFullName, FileMode.Create))
                    {
                        formFile.CopyTo(fs);
                        fs.Flush();
                    }
                }
                filePathList.Add("src/imgs/" + fileName);
            }

            return Success(filePathList);
        }
    }
}