﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using LIMS.MVCFoundation.Core;
using LIMS.MVCFoundation.Controllers;
using LIMS.Services;
using LIMS.Entities;
using LIMS.Models;
using LIMS.Util;
using LIMS.MVCFoundation.Attributes;


namespace LIMS.Web.Controllers.Setting
{
    [RequiredLogon]
    [BaseEntityValue]
    public class VendorSettingController : BaseController
    {

        public ActionResult Vendors()
        {
            return View();
        }

        public ActionResult VendorEdit(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var mode = new UnitService().Get(id);
                return View(mode);
            }

            return View();
        }

        /// <summary>
        /// 根据ID获取单位信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult UnitQueryById(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                return JsonNet(new ResponseResult(false,null, ErrorCodes.RequireField));
            }
            var mode = new UnitService().Get(id);
            return JsonNet(new ResponseResult(true,mode));
        }

        /// <summary>
        /// 保存供应商信息
        /// </summary>
        /// <param name="vendor"></param>
        /// <returns></returns>
        public JsonNetResult SaveVendor(UnitModel vendor)
        {
            if (vendor == null)
            {
                throw new ArgumentNullException("The vendor is null.");
            }

            if (!this.Validate(vendor))
            {
                return JsonNet(new ResponseResult(false, "The required attributes of vendor are not filled.", ErrorCodes.RequireField));
            }
            if (UserContext.UnitType == UnitType.Vendor && UserContext.RootUnitId != vendor.Id)
            {
                return JsonNet(new ResponseResult(false, "只可修改本供应商信息", ErrorCodes.RequireField));
            }


            new UnitService().Save(new UnitEntity
            {
                Id = vendor.Id,
                Name = vendor.Name,
                Type = UnitType.Vendor,
                Description = vendor.Description,
                ShortCode = vendor.ShortCode
            }, new ContactInfoEntity
            {
                Id = vendor.ContactInfo.Id,
                ContactPerson = vendor.ContactInfo.ContactPerson,
                Address = vendor.ContactInfo.Address,
                ContactWay1 = vendor.ContactInfo.ContactWay1,
                ContactWay2 = vendor.ContactInfo.ContactWay2,
                ContactWay3 = vendor.ContactInfo.ContactWay3,
                ContactWay4 = vendor.ContactInfo.ContactWay4
            });

            return JsonNet(new ResponseResult());
        }

        /// <summary>
        /// 供应商分页查询
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="pager"></param>
        /// <returns></returns>
        public JsonNetResult QueryVendors(string condition, PagerInfo pager)
        {
            try
            {
                var service = new UnitService();
                if (!this.IsAdmin)
                {
                    condition = UserContext.RootUnitName;
                }
                var list = service.QueryRoots(condition, UnitType.Vendor, pager);

                var result = new List<UnitModel>();
                foreach (var item in list)
                {
                    result.Add(new UnitModel
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Description = item.Description,
                        ShortCode = item.ShortCode,
                        ContactId = item.ContactId
                    });
                }

                return JsonNet(new ResponseResult(true, result, pager));
            }
            catch(Exception e)
            {
                return JsonNet(new ResponseResult(e));
            }
        }

        public ActionResult VendorUnits()
        {
            var vendors = new UnitService().QueryRoots(UnitType.Vendor);
            ViewBag.Vendors = vendors.Select(item => new { Id = item.Id, Name = item.Name });
            return View();
        }

        /// <summary>
        /// 获取供应商名称和ID
        /// </summary>
        /// <returns></returns>
        public JsonNetResult JsonVendorUnits()
        {
            var vendors = new UnitService().QueryRoots(UnitType.Vendor);
            return JsonNet(new ResponseResult(true, vendors.Select(item => new { Id = item.Id, Name = item.Name })));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vendorId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult VendorUnitEdit(string vendorId, string id)
        {
            ViewBag.VendorId = vendorId;
            if (!string.IsNullOrEmpty(id))
            {
                var unit = new UnitService().Get(id);
                return View(unit);
            }

            return View();
        }

        /// <summary>
        /// 保存供应商单位信息
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public JsonNetResult SaveVendorUnit(UnitModel unit)
        {
            if (unit == null)
            {
                throw new ArgumentNullException("The vendor unit is null.");
            }

            if (!this.Validate(unit))
            {
                return JsonNet(new ResponseResult(false, "The required attributes of vendor unit are not filled.", ErrorCodes.RequireField));
            }

            new UnitService().Save(new UnitEntity
            {
                Id = unit.Id,
                Name = unit.Name,
                Type = UnitType.VendorUnit,
                Description = unit.Description,
                ShortCode = unit.ShortCode,
                ParentId = unit.ParentId,
                RootId = unit.ParentId
            }, new ContactInfoEntity
            {
                Id = unit.ContactInfo.Id,
                ContactPerson = unit.ContactInfo.ContactPerson,
                Address = unit.ContactInfo.Address,
                ContactWay1 = unit.ContactInfo.ContactWay1,
                ContactWay2 = unit.ContactInfo.ContactWay2,
                ContactWay3 = unit.ContactInfo.ContactWay3,
                ContactWay4 = unit.ContactInfo.ContactWay4
            });

            return JsonNet(new ResponseResult());
        }

        private bool Validate(UnitModel mode)
        {
            if (string.IsNullOrEmpty(mode.Name))
            {
                return false;
            }

            if (string.IsNullOrEmpty(mode.ContactInfo.ContactPerson))
            {
                return false;
            }

            return true;
        }


        #region products of vendor
        public ActionResult VendorProducts(string id)
        {
            ViewBag.Context = new
            {
                VendorId = id,
                Units = new UnitService().GetByRootId(id).Select(item => new { Id = item.Id, Name = item.Name }),
                Products = new ProductService().Query().Select(item => new { Id = item.Id, Name = item.Name })
            };

            return View();
        }

        /// <summary>
        /// 获取供应商产品
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonNetResult JsonVendorProducts(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new Exception("The id unit is empty.");
            }
            var units = new UnitService().GetByRootId(id).Select(item => new {Id = item.Id, Name = item.Name});
            var products = new ProductService().Query().Select(item => new {Id = item.Id, Name = item.Name});
            return JsonNet(new ResponseResult(true, new { units= units, products= products }));
        }

        public JsonNetResult GetVendorProduct(string unitId, string productId)
        {
            try
            {
                var entity = new VendorProductService().Get(unitId, productId);

                return JsonNet(new ResponseResult(true, entity));
            }
            catch(Exception e)
            {
                return JsonNet(new ResponseResult(e));
            }
        }

        /// <summary>
        /// 获取产品信息
        /// </summary>
        /// <param name="unitId">科室ID</param>
        /// <param name="productId">产品ID</param>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult JsonGetVendorProduct(string unitId, string productId)
        {
            try
            {
                var entity = new VendorProductService().Get(unitId, productId);
                var product = new ProductService().GetByHospital(productId);
                return JsonNet(new ResponseResult(true, new
                {
                    entity = entity,
                    product = product,
                    IsUsing = entity == null
                }));
            }
            catch (Exception e)
            {
                return JsonNet(new ResponseResult(e));
            }
        }

        public JsonNetResult SaveVendorProduct(VendorProductEntity entity)
        {
            try
            {
                if (string.IsNullOrEmpty(entity.UnitId))
                {
                    throw new Exception("The vendor unit is empty.");
                }

                if (string.IsNullOrEmpty(entity.ProductId))
                {
                    throw new Exception("The product is empty.");
                }

                new VendorProductService().Save(entity);

                return JsonNet(new ResponseResult(true, entity));
            }
            catch(Exception e)
            {
                return JsonNet(new ResponseResult(e));
            }
        }
        #endregion


        public ActionResult VendorHospitals(string id)
        {
            ViewBag.Context = new
            {
                VendorId = id,
                Hospitals = new UnitService().QueryRoots(UnitType.Hospital),
                SelectedHospitals = new VendorHospitalService().GetByVendor(id)
            };

            return View();
        }

        /// <summary>
        /// 获取供应商关联的医院
        /// </summary>
        /// <param name="id">供应商ID</param>
        /// <returns></returns>
        public ActionResult JsonVendorHospitals(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new Exception("The id unit is empty.");
            }
            var hospitals = new UnitService().QueryRoots(UnitType.Hospital).ToList();
            var selectedHospitals = new VendorHospitalService().GetByVendor(id);
            var notSelectedHospitals = new List<UnitEntity>();
            var selectedHospital = new List<UnitEntity>();
            hospitals.ForEach(m =>
            {
                if (selectedHospitals.All(j => j.HospitalId != m.Id))
                {
                    notSelectedHospitals.Add(m);
                }
                else
                {
                    selectedHospital.Add(m);
                }
            });
            return JsonNet(new ResponseResult(true, new { vendorId=id, notSelectedHospitals = notSelectedHospitals, selectedHospitals = selectedHospital }));
        }

        public JsonNetResult SaveVendorHospitals(string vendorId, IList<string> hospitals)
        {
            try
            {
                if (string.IsNullOrEmpty(vendorId))
                {
                    throw new Exception("The vendor is empty.");
                }

                var unit = new UnitService().Get(vendorId);
                if (unit == null)
                {
                    throw new Exception("The vendor does not exist.");
                }

                var entities = new List<VendorHospitalEntity>();
                if (hospitals != null)
                {
                    foreach (var id in hospitals)
                    {
                        entities.Add(new VendorHospitalEntity
                        {
                            Id = Guid.NewGuid().ToString(),
                            VendorId = vendorId,
                            HospitalId = id
                        });
                    }
                }

                new VendorHospitalService().Save(vendorId, entities);

                return JsonNet(new ResponseResult());
            }
            catch (Exception e)
            {
                return JsonNet(new ResponseResult(e));
            }
        }
    }
}
