﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.MasterData.Repositories;
using PurchasingSystemApps.Areas.MasterData.ViewModels;
using PurchasingSystemApps.Areas.Order.Repositories;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;
using System.Data;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemApps.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class UserActiveController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IWarehouseLocationRepository _warehouseLocationRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public UserActiveController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IUserActiveRepository userActiveRepository,
            IWarehouseLocationRepository warehouseLocationRepository,
            IDepartmentRepository departmentRepository, 
            IPositionRepository positionRepository,
            IPurchaseRequestRepository purchaseRequestRepository,
            IPurchaseOrderRepository purchaseOrderRepository,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _userActiveRepository = userActiveRepository;
            _warehouseLocationRepository = warehouseLocationRepository;
            _departmentRepository = departmentRepository;
            _positionRepository = positionRepository;
            _purchaseRequestRepository = purchaseRequestRepository;
            _purchaseOrderRepository = purchaseOrderRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        public JsonResult LoadPosition(Guid Id)
        {
            var position = _applicationDbContext.Positions.Where(p => p.DepartmentId == Id).ToList();
            return Json(new SelectList(position, "PositionId", "PositionName"));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _userActiveRepository.GetAllUser();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "MasterData";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _userActiveRepository.GetAllUser().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();            
            return View(data);
        }

        [HttpGet]
        public async Task<ViewResult> CreateUserActive()
        {
            ViewBag.Active = "MasterData";

            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);

            var user = new UserActiveViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _userActiveRepository.GetAllUser().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.UserActiveCode).FirstOrDefault();
            if (lastCode == null)
            {
                user.UserActiveCode = "USR" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.UserActiveCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    user.UserActiveCode = "USR" + setDateNow + "0001";
                }
                else
                {
                    user.UserActiveCode = "USR" + setDateNow + (Convert.ToInt32(lastCode.UserActiveCode.Substring(9, lastCode.UserActiveCode.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUserActive(UserActiveViewModel vm)
        {
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _userActiveRepository.GetAllUser().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.UserActiveCode).FirstOrDefault();
            if (lastCode == null)
            {
                vm.UserActiveCode = "USR" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.UserActiveCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    vm.UserActiveCode = "USR" + setDateNow + "0001";
                }
                else
                {
                    vm.UserActiveCode = "USR" + setDateNow + (Convert.ToInt32(lastCode.UserActiveCode.Substring(9, lastCode.UserActiveCode.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                string uniqueFileName = ProcessUploadFile(vm);

                var userLogin = new ApplicationUser
                {
                    KodeUser = vm.UserActiveCode,
                    NamaUser = vm.FullName,
                    Email = vm.Email,
                    UserName = vm.Email,
                    IsActive = true
                };

                var user = new UserActive
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    UserActiveId = vm.UserActiveId,
                    UserActiveCode = vm.UserActiveCode,
                    FullName = vm.FullName,
                    IdentityNumber = vm.IdentityNumber,
                    DepartmentId = vm.DepartmentId,
                    PositionId = vm.PositionId,
                    PlaceOfBirth = vm.PlaceOfBirth,
                    DateOfBirth = vm.DateOfBirth,
                    Gender = vm.Gender,
                    Address = vm.Address,
                    Handphone = vm.Handphone,
                    Email = vm.Email,
                    Foto = uniqueFileName,
                    IsActive = true
                };

                var passTglLahir = vm.DateOfBirth.ToString("ddMMMyyyy");                
                var resultLogin = await _userManager.CreateAsync(userLogin, passTglLahir);

                var checkDuplicatePengguna = _userActiveRepository.GetAllUser().Where(c => c.FullName == vm.FullName && c.DepartmentId == vm.DepartmentId).ToList();

                if (checkDuplicatePengguna.Count == 0)
                {
                    var resultPengguna = _userActiveRepository.GetAllUser().Where(c => c.FullName == vm.FullName).FirstOrDefault();

                    if (resultPengguna == null)
                    {
                        if (resultLogin.Succeeded)
                        {
                            _userActiveRepository.Tambah(user);
                            TempData["SuccessMessage"] = "Account " + vm.FullName + " Saved";
                            return RedirectToAction("Index", "UserActive");
                        }
                        else
                        {
                            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                            TempData["WarningMessage"] = "Account " + vm.FullName + " Already Exist !!!";
                            return View(vm);
                        }
                    }
                    else
                    {
                        ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                        ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                        TempData["WarningMessage"] = "Account " + vm.FullName + " Already Exist !!!";
                        return View(vm);
                    }
                }
                else
                {
                    ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                    ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                    TempData["WarningMessage"] = "Account " + vm.FullName + " There is duplicate data !!!";
                    return View(vm);
                }
            }
            else
            {
                ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);                
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailUserActive(Guid Id)
        {
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);

            ViewBag.Active = "MasterData";
            var user = await _userActiveRepository.GetUserById(Id);

            if (user == null)
            {
                Response.StatusCode = 404;
                return View("UserNotFound", Id);
            }

            UserActiveViewModel viewModel = new UserActiveViewModel
            {
                UserActiveId = user.UserActiveId,
                UserActiveCode = user.UserActiveCode,
                FullName = user.FullName,
                IdentityNumber = user.IdentityNumber,
                DepartmentId = user.DepartmentId,
                PositionId = user.PositionId,
                PlaceOfBirth = user.PlaceOfBirth,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Address = user.Address,
                Handphone = user.Handphone,
                Email = user.Email,
                UserPhotoPath = user.Foto,
                IsActive = user.IsActive
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DetailUserActive(UserActiveViewModel viewModel)
        {
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);

            if (ModelState.IsValid)
            {
                var user = await _userActiveRepository.GetUserByIdNoTracking(viewModel.UserActiveId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var userLogin = await _userManager.FindByNameAsync(viewModel.Email);
                var checkDuplicate = _userActiveRepository.GetAllUser().Where(d => d.UserActiveCode == viewModel.UserActiveCode).ToList();

                if (checkDuplicate.Count == 0 || checkDuplicate.Count == 1)
                {
                    var data = _userActiveRepository.GetAllUser().Where(d => d.UserActiveCode == viewModel.UserActiveCode).FirstOrDefault();

                    if (data != null)
                    {
                        if (userLogin != null)
                        {
                            user.UpdateDateTime = DateTime.Now;
                            user.UpdateBy = new Guid(getUser.Id);
                            user.UserActiveCode = viewModel.UserActiveCode;
                            user.FullName = viewModel.FullName;
                            user.IdentityNumber = viewModel.IdentityNumber;
                            user.DepartmentId = viewModel.DepartmentId;
                            user.PositionId = viewModel.PositionId;
                            user.PlaceOfBirth = viewModel.PlaceOfBirth;
                            user.DateOfBirth = viewModel.DateOfBirth;
                            user.Gender = viewModel.Gender;
                            user.Address = viewModel.Address;
                            user.Handphone = viewModel.Handphone;
                            user.Email = viewModel.Email;
                            user.IsActive = viewModel.IsActive;

                            if (viewModel.Foto != null)
                            {
                                if (viewModel.UserPhotoPath != null)
                                {
                                    string filePath = Path.Combine(_hostingEnvironment.WebRootPath,
                                        "UserPhoto", viewModel.UserPhotoPath);
                                    System.IO.File.Delete(filePath);
                                }
                                user.Foto = ProcessUploadFile(viewModel);
                            }

                            userLogin.NamaUser = viewModel.FullName;
                            userLogin.UserName = viewModel.Email;
                            userLogin.Email = viewModel.Email;
                            userLogin.IsActive = viewModel.IsActive;

                            var result = await _userManager.UpdateAsync(userLogin);

                            if (result.Succeeded)
                            {
                                _userActiveRepository.Update(user);
                                _applicationDbContext.SaveChanges();

                                TempData["SuccessMessage"] = "Account " + viewModel.FullName + " Success Changes";
                                return RedirectToAction("Index", "UserActive");
                            }
                        }
                        else
                        {
                            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                            TempData["WarningMessage"] = "Account " + viewModel.FullName + " Sorry, Data Failed !!!";
                            return View(viewModel);
                        }
                    }
                    else
                    {
                        ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                        ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                        TempData["WarningMessage"] = "Account " + viewModel.FullName + " Already Exist !!!";
                        return View(viewModel);
                    }
                }
                else
                {
                    ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                    ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                    TempData["WarningMessage"] = "Account " + viewModel.FullName + " There is duplicate data !!!";
                    return View(viewModel);
                }
            }
            else
            {
                ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);                
                return View(viewModel);
            }

            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUserActive(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var user = await _userActiveRepository.GetUserById(Id);
            if (user == null)
            {
                Response.StatusCode = 404;
                return View("UserNotFound", Id);
            }

            UserActiveViewModel vm = new UserActiveViewModel
            {
                UserActiveId = user.UserActiveId,
                UserActiveCode = user.UserActiveCode,
                FullName = user.FullName,
                UserPhotoPath = user.Foto,
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUserActive(UserActiveViewModel vm)
        {
            //Cek Relasi
            var userPurchaseRequest = _purchaseRequestRepository.GetAllPurchaseRequest().Where(p => p.UserAccessId == vm.UserActiveId.ToString()).FirstOrDefault();
            var userPurchaseOrder = _purchaseOrderRepository.GetAllPurchaseOrder().Where(p => p.UserAccessId == vm.UserActiveId.ToString()).FirstOrDefault();
            var userWarehouse = _warehouseLocationRepository.GetAllWarehouseLocation().Where(p => p.WarehouseManagerId == vm.UserActiveId).FirstOrDefault();
            
            if (userWarehouse == null)
            {
                //Hapus Akun Login
                var userLogin = _signInManager.UserManager.Users.FirstOrDefault(s => s.KodeUser == vm.UserActiveCode);
                _applicationDbContext.Attach(userLogin);
                _applicationDbContext.Entry(userLogin).State = EntityState.Deleted;
                _applicationDbContext.SaveChanges();

                //Hapus Data Profil
                var userActive = _applicationDbContext.UserActives.FirstOrDefault(x => x.UserActiveId == vm.UserActiveId);
                _applicationDbContext.Attach(userActive);
                _applicationDbContext.Entry(userActive).State = EntityState.Deleted;
                _applicationDbContext.SaveChanges();

                if (vm.UserPhotoPath != null)
                {
                    string filePath = Path.Combine(_hostingEnvironment.WebRootPath,
                        "UserPhoto", vm.UserPhotoPath);
                    System.IO.File.Delete(filePath);
                }

                userActive.Foto = ProcessUploadFile(vm);

                TempData["SuccessMessage"] = "Account " + vm.FullName + " Success Deleted";

                return RedirectToAction("Index", "UserActive");
            }
            else if (userPurchaseRequest == null)
            {
                //Hapus Akun Login
                var userLogin = _signInManager.UserManager.Users.FirstOrDefault(s => s.KodeUser == vm.UserActiveCode);
                _applicationDbContext.Attach(userLogin);
                _applicationDbContext.Entry(userLogin).State = EntityState.Deleted;
                _applicationDbContext.SaveChanges();

                //Hapus Data Profil
                var userActive = _applicationDbContext.UserActives.FirstOrDefault(x => x.UserActiveId == vm.UserActiveId);
                _applicationDbContext.Attach(userActive);
                _applicationDbContext.Entry(userActive).State = EntityState.Deleted;
                _applicationDbContext.SaveChanges();

                if (vm.UserPhotoPath != null)
                {
                    string filePath = Path.Combine(_hostingEnvironment.WebRootPath,
                        "UserPhoto", vm.UserPhotoPath);
                    System.IO.File.Delete(filePath);
                }

                userActive.Foto = ProcessUploadFile(vm);

                TempData["SuccessMessage"] = "Account " + vm.FullName + " Success Deleted";

                return RedirectToAction("Index", "UserActive");
            }
            else if (userPurchaseOrder == null)
            {
                //Hapus Akun Login
                var userLogin = _signInManager.UserManager.Users.FirstOrDefault(s => s.KodeUser == vm.UserActiveCode);
                _applicationDbContext.Attach(userLogin);
                _applicationDbContext.Entry(userLogin).State = EntityState.Deleted;
                _applicationDbContext.SaveChanges();

                //Hapus Data Profil
                var userActive = _applicationDbContext.UserActives.FirstOrDefault(x => x.UserActiveId == vm.UserActiveId);
                _applicationDbContext.Attach(userActive);
                _applicationDbContext.Entry(userActive).State = EntityState.Deleted;
                _applicationDbContext.SaveChanges();

                if (vm.UserPhotoPath != null)
                {
                    string filePath = Path.Combine(_hostingEnvironment.WebRootPath,
                        "UserPhoto", vm.UserPhotoPath);
                    System.IO.File.Delete(filePath);
                }

                userActive.Foto = ProcessUploadFile(vm);

                TempData["SuccessMessage"] = "Account " + vm.FullName + " Success Deleted";

                return RedirectToAction("Index", "UserActive");
            }
            else
            {
                TempData["WarningMessage"] = "Sorry, " + vm.FullName + " In used by the warehouse location !";
                return View(vm);
            }
        }
        private string ProcessUploadFile(UserActiveViewModel model)
        {
            string uniqueFileName = null;
            if (model.Foto != null)
            {
                string uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "UserPhoto");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }
                uniqueFileName = Guid.NewGuid().ToString() + "_" + model.FullName + "_" + model.Foto.FileName;
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                model.Foto.CopyTo(new FileStream(filePath, FileMode.Create));
            }

            return uniqueFileName;
        }
    }
}