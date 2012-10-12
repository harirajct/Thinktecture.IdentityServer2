﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Thinktecture.IdentityServer.Models;
using Thinktecture.IdentityServer.Repositories;
using Thinktecture.IdentityServer.Web.Areas.Admin.ViewModels;

namespace Thinktecture.IdentityServer.Web.Areas.Admin.Controllers
{
    [ClaimsAuthorize(Constants.Actions.Administration, Constants.Resources.Configuration)]
    public class IPController : Controller
    {
        [Import]
        public IIdentityProviderRepository identityProviderRepository { get; set; }

        public IPController()
        {
            Container.Current.SatisfyImportsOnce(this);
        }
        public IPController(IIdentityProviderRepository identityProviderRepository)
        {
            this.identityProviderRepository = identityProviderRepository;
        }

        public ActionResult Index()
        {
            var vm = new IdentityProvidersViewModel(this.identityProviderRepository);
            return View("Index", vm);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string action, IPModel[] list)
        {
            if (action == "delete") return Delete(list);
            if (action == "new") return Create();
            ModelState.AddModelError("", "Invalid action.");
            var vm = new IdentityProvidersViewModel(this.identityProviderRepository);
            return View("Index", vm);
        }
        
        [ChildActionOnly]
        public ActionResult Menu()
        {
            var list = new IdentityProvidersViewModel(this.identityProviderRepository);
            if (list.IdentityProviders.Any())
            {
                var vm = new ChildMenuViewModel
                {
                    Items = list.IdentityProviders.Select(x =>
                        new ChildMenuItem
                        {
                            Controller = "IP",
                            Action = "IP",
                            Title = x.DisplayName,
                            RouteValues = new { id = x.Name }
                        }).ToArray()
                };
                return PartialView("ChildMenu", vm);
            }
            return new EmptyResult();
        }

        ActionResult Delete(IPModel[] list)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (list != null)
                    {
                        foreach (var item in list.Where(x => x.Delete))
                        {
                            this.identityProviderRepository.Delete(item.Name);
                        }
                        TempData["Message"] = "Identity Providers Deleted.";
                    }
                    return RedirectToAction("Index");
                }
                catch (ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch
                {
                    ModelState.AddModelError("", "Error updating identity providers.");
                }
            }
            var vm = new IdentityProvidersViewModel(this.identityProviderRepository);
            return View("Index", vm);
        }

        private ActionResult Create()
        {
            return View("IP", new IdentityProvider());
        }

        public ActionResult IP(string id)
        {
            IdentityProvider ip;
            if (!this.identityProviderRepository.TryGet(id, out ip)) return HttpNotFound();

            return View("IP", ip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IdentityProvider model, IPCertInputModel cert)
        {
            if (cert != null && cert.Cert != null)
            {
                model.IssuerThumbprint = cert.Cert.Thumbprint;
                if (model.IssuerThumbprint != null)
                {
                    ModelState["IssuerThumbprint"].Errors.Clear();
                    ModelState["IssuerThumbprint"].Value = new ValueProviderResult(model.IssuerThumbprint, model.IssuerThumbprint, ModelState["IssuerThumbprint"].Value.Culture);
                }
            } 

            if (ModelState.IsValid)
            {
                try
                {
                    this.identityProviderRepository.Add(model);
                    TempData["Message"] = "Identity Provider Created";
                    return RedirectToAction("IP", new { id=model.Name });
                }
                catch (ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch
                {
                    ModelState.AddModelError("", "Error updating identity provider.");
                }
            }
            
            return View("IP", model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(IdentityProvider model, IPCertInputModel cert)
        {
            if (cert != null && cert.Cert != null)
            {
                model.IssuerThumbprint = cert.Cert.Thumbprint;
                ModelState["IssuerThumbprint"].Errors.Clear();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    this.identityProviderRepository.Update(model);
                    TempData["Message"] = "Identity Provider Updated"; ;
                    return RedirectToAction("IP", new { id = model.Name });
                }
                catch (ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch
                {
                    ModelState.AddModelError("", "Error updating identity provider.");
                }
            }
            
            return View("IP", model);
        }
    }
}