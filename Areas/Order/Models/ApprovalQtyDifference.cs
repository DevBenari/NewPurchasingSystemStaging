﻿using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.Warehouse.Models;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemApps.Areas.Order.Models
{
    [Table("OrdApprovalQtyDifference", Schema = "dbo")]
    public class ApprovalQtyDifference : UserActivity
    {
        public Guid ApprovalQtyDifferenceId { get; set; }
        public Guid? QtyDifferenceId { get; set; }
        public string QtyDifferenceNumber { get; set; }
        public string UserAccessId { get; set; } //Dibuat Oleh
        public Guid? PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; }        
        public Guid? UserApproveId { get; set; }
        public string ApproveBy { get; set; }
        public string? ApprovalTime { get; set; }
        public DateTimeOffset ApprovalDate { get; set; }
        public string? ApprovalStatusUser { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public string? Message { get; set; }

        //Relationship
        [ForeignKey("QtyDifferenceId")]
        public QtyDifference? QtyDifference { get; set; }
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder? PurchaseOrder { get; set; }
        [ForeignKey("UserApproveId")]
        public UserActive? UserApprove { get; set; }
        [ForeignKey("UserAccessId")]
        public ApplicationUser? ApplicationUser { get; set; }
    }
}