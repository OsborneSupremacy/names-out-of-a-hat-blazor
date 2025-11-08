resource "null_resource" "build" {
  triggers = {
    timestamp = timestamp()
  }

  provisioner "local-exec" {
    command = local.build_command
  }
}

data "archive_file" "lambda_function" {
  type        = "zip"
  source_dir  = local.build_output_path
  output_path = local.publish_zip_path
  depends_on  = [null_resource.build]
}
