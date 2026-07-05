variable "environment" {
  description = "Deployment environment (dev, staging, prod)"
  type        = string
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

variable "region" {
  description = "Cloud provider region"
  type        = string
}

variable "redis_sku" {
  description = "Redis cache SKU"
  type        = string
  default     = "Basic"
}

variable "container_registry" {
  description = "Container registry URL for pulling images"
  type        = string
}
