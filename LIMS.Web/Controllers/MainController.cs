﻿using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using LIMS.Entities;
using LIMS.Models;
using LIMS.MVCFoundation.Attributes;
using LIMS.MVCFoundation.Controllers;
using LIMS.MVCFoundation.Core;
using LIMS.Services;
using LIMS.Util;

namespace LIMS.Web.Controllers
{
    [RequiredLogon]
    public class MainController : BaseController
    {

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 左侧菜单
        /// </summary>
        /// <returns></returns>
        public PartialViewResult Menus()
        {
            var mainMenus = new MainMenusModel
            {
                Menus = new List<MenuModel>(),
                IsAdmin = this.IsAdmin
            };

            IList<SystemFunctionEntity> funs;
            if (this.IsAdmin)
            {
                funs = new List<SystemFunctionEntity>();
                funs.Add(new SystemFunctionService().GetSettingFunction());
            }
            else
            {
                if (this.UserContext.HospitalOrVendor)
                {
                    var hospitalId = string.IsNullOrEmpty(this.UserContext.CurrentHospital)
                        ? this.UserContext.RootUnitId : this.UserContext.CurrentHospital;
                    funs = new SystemFunctionService().GetUserFunctions(hospitalId, this.UserContext.UserId);
                }
                else
                {
                    funs = new SystemFunctionService().GetUserFunctions(this.UserContext.RootUnitId, this.UserContext.UserId);
                }
            }

            foreach (var fun in funs)
            {
                var menu = new MenuModel
                {
                    Id = fun.Id,
                    Title = fun.Title,
                    Url = fun.Url,
                    SubMenus = new List<MenuModel>()
                };
                menu.SubMenus = fun.SubFunctions.Select(item => new MenuModel
                {
                    Id = item.Id,
                    Title = item.Title,
                    Url = item.Url
                }).ToList();

                mainMenus.Menus.Add(menu);
            }

            return PartialView("~/Views/Main/_Menus.cshtml", mainMenus);
        }

        /// <summary>
        /// 左侧菜单以及用户信息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult JsonMenus()
        {
            var mainMenus = new MainMenusModel
            {
                Menus = new List<MenuModel>(),
                IsAdmin = this.IsAdmin,
                UserAccount = UserContext.Account,
                UserName = UserContext.Name
            };

            IList<SystemFunctionEntity> funs;
            if(this.IsAdmin)
            {
                funs = new List<SystemFunctionEntity>();
                funs.Add(new SystemFunctionService().GetSettingFunction());
            }
            else
            {
                if (this.UserContext.HospitalOrVendor)
                {
                    var hospitalId = string.IsNullOrEmpty(this.UserContext.CurrentHospital)
                        ? this.UserContext.RootUnitId : this.UserContext.CurrentHospital;
                    funs = new SystemFunctionService().GetUserFunctions(hospitalId, this.UserContext.UserId);
                }
                else
                {
                    funs = new SystemFunctionService().GetUserFunctions(this.UserContext.RootUnitId, this.UserContext.UserId);
                }
            }
            foreach(var fun in funs)
            {
                var menu = new MenuModel
                {
                    Id = fun.Id,
                    Title = fun.Title,
                    Url = fun.Url,
                    SubMenus = new List<MenuModel>()
                };
                menu.SubMenus = fun.SubFunctions.Select(item => new MenuModel
                {
                    Id = item.Id,
                    Title = item.Title,
                    Url = item.Url
                }).ToList();

                mainMenus.Menus.Add(menu);
            }
            var loginInfo = LoginInfo();
            return  JsonNet(new ResponseResult(true,new { mainMenus, loginInfo }));
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        private LoginInfoModel LoginInfo()
        {
            var loginInfo = new LoginInfoModel
            {
                IsAdmin = this.IsAdmin,
                UserName = this.UserContext.Name
            };

            var hospitalId = string.Empty;
            if (this.UserContext.HospitalOrVendor)
            {
                hospitalId = string.IsNullOrEmpty(this.UserContext.CurrentHospital)
                    ? this.UserContext.RootUnitId : this.UserContext.CurrentHospital;

                var found = false;
                loginInfo.Hospitals = new UnitService().GetHospitalsByUserId(this.UserContext.UserId).Select(item =>
                {
                    var result = new TargetHospitalModel
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Selected = string.Compare(hospitalId, item.Id, true) == 0
                    };

                    if (!found)
                    {
                        found = result.Selected;
                    }
                    return result;
                }).ToList();

                hospitalId = found ? hospitalId : this.UserContext.RootUnitId;
            }
            else
            {
                loginInfo.Hospitals = new UnitService().GetHospitalsByVendor(this.UserContext.RootUnitId).Select(item =>
                new TargetHospitalModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Selected = string.Compare(this.UserContext.CurrentHospital, item.Id, true) == 0
                }).ToList();

                hospitalId = string.IsNullOrEmpty(this.UserContext.CurrentHospital)
                    ? (loginInfo.Hospitals.Count > 0 ? loginInfo.Hospitals[0].Id : "") : this.UserContext.CurrentHospital;
                
            }

            InitCookie(hospitalId);
            return loginInfo;
        }

        /// <summary>
        ///刷新cookic信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonNetResult ChangeHospital(string id)
        {
            this.InitCookie(id);

            return JsonNet(new ResponseResult());
        }

        /// <summary>
        /// 注销登录
        /// </summary>
        /// <returns></returns>
        public ActionResult JsonLogout()
        {
            this.ClearContext();
            FormsAuthentication.SignOut();
            return Json(new { IsSuccess = true });
        }

        public ActionResult Logout()
        {
            this.ClearContext();
            FormsAuthentication.SignOut();

            return new RedirectResult("~/Logon");
        }

        private void InitCookie(string hospitalId)
        {
            HttpCookie cookie = this.Request.Cookies[Constant.CURRENT_HOSPITAL_COOKIE];
            if (cookie == null)
            {
                cookie = new HttpCookie(Constant.CURRENT_HOSPITAL_COOKIE, hospitalId);
            }
            else
            {
                cookie.Value = hospitalId;
            }
            this.Request.Cookies.Remove(Constant.CURRENT_HOSPITAL_COOKIE);

            cookie.HttpOnly = false;
            this.Response.Cookies.Add(cookie);
        }
    }
}
