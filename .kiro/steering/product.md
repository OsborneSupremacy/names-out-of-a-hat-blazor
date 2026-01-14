# Product Overview

**Names Out Of A Hat** is a serverless web application for organizing remote gift exchanges where participants draw names randomly.

## Core Functionality

- Organizers create "hats" (gift exchanges) and invite participants
- Participants are assigned random gift recipients with configurable eligibility rules
- Prevents participants from drawing themselves or ineligible recipients (e.g., spouses)
- Supports complex eligibility matrices for advanced scenarios
- Email-based invitation and notification system
- Organizer verification workflow before assignments

## Key Features

- Remote gift exchange coordination
- Flexible ineligibility rules per participant
- Brute-force name assignment algorithm with retry logic
- Email notifications via AWS SES
- Serverless architecture using AWS Lambda, API Gateway, DynamoDB, and SQS

## Architecture

Backend is serverless AWS infrastructure deployed via Terraform:
- AWS Lambda functions handle API requests and background processing
- API Gateway provides REST endpoints
- DynamoDB stores hat and participant data
- SQS queues invitation emails for async processing
- AWS SES sends email notifications
