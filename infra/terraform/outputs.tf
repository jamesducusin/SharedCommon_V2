output "redis_connection_string" {
  description = "Redis connection string (sensitive)"
  value       = "placeholder_redis_connection"
  sensitive   = true
}

output "kafka_bootstrap_servers" {
  description = "Kafka bootstrap server addresses"
  value       = "placeholder_kafka_servers"
}
