﻿using AutoMapper;
using Blink_API.DTOs.BranchDto;
using Blink_API.DTOs.BrandDtos;
using Blink_API.DTOs.IdentityDTOs.UserDTOs;
using Blink_API.DTOs.UsersDtos;
using Blink_API.Errors;
using Blink_API.Models;
using Microsoft.AspNetCore.Identity;
using UserDto = Blink_API.DTOs.UsersDtos.UserDto;

namespace Blink_API.Services.UserService
{
    public class UserService
    {
        private readonly UnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly UserManager<ApplicationUser> userManager;

        public UserService(UnitOfWork _unitOfWork, IMapper _mapper, UserManager<ApplicationUser> _userManager)
        {
            unitOfWork = _unitOfWork;
            mapper = _mapper;
            userManager = _userManager;
        }

        // get all
        public async Task<ICollection<UserDto>> GetAllUsers()
        {
            var users = await unitOfWork.UserRepo.GetAll();
            var usersDto = mapper.Map<List<UserDto>>(users);

            for (int i = 0; i < users.Count; i++)
            {
                var roles = await userManager.GetRolesAsync(users[i]);
                usersDto[i].Role = roles.FirstOrDefault();
            }

            return usersDto;
            // return mapper.Map<ICollection<UserDto>>(users);
        }

        // get by id :
        public async Task<UserDto?> GetUserById(string id)
        {
            var user = await unitOfWork.UserRepo.GetById(id);
            if (user == null) return null;
            var userDto = mapper.Map<UserDto>(user);
            var roles = await userManager.GetRolesAsync(user);
            userDto.Role = roles.FirstOrDefault();

            return userDto;
            // return mapper.Map<UserDto>(user);
        }
        // get by name :
        public async Task<List<UserDto>> GetUserByName(string name)
        {
            var users = await unitOfWork.UserRepo.GetByUserName(name);
            if (users == null || !users.Any()) return new List<UserDto>();
            var usersDto = mapper.Map<List<UserDto>>(users);
            for (int i = 0; i < users.Count; i++)
            {
                var roles = await userManager.GetRolesAsync(users[i]);
                usersDto[i].Role = roles.FirstOrDefault();
            }

            return usersDto;
        }

        // insert :
        public async Task<ApiResponse> InsertUser(AddUserDto insertedUser)
        {
            if (insertedUser == null)
            {
                throw new ArgumentException("Invalid user, please try again ! ");
            }
            var user = mapper.Map<ApplicationUser>(insertedUser);
            var result = await userManager.CreateAsync(user, insertedUser.UserPassword);
//            unitOfWork.UserRepo.Add(user);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(insertedUser.Role))
                {
                    await userManager.AddToRoleAsync(user, insertedUser.Role);
                }
                //unitOfWork.UserRepo.Add(user);
                //await unitOfWork.UserRepo.SaveChanges();

                return new ApiResponse(201, "User created successfully.");
            }
            else
            {
                return new ApiResponse(400, "Failed to create user.");
            }
        }
        // Update  :
        public async Task<ApiResponse> UpdateUser(string id, AddUserDto updatedUser)
        {
            if (updatedUser == null)
            {
                return new ApiResponse(400, "Invalid user data.");
            }
            var oldUser = await unitOfWork.UserRepo.GetById(id);
            if (oldUser != null)
            {
                oldUser.FirstName = updatedUser.FirstName;
                oldUser.LastName = updatedUser.LastName;
                oldUser.UserName = updatedUser.UserName;
                oldUser.Email = updatedUser.Email;
                oldUser.PhoneNumber = updatedUser.PhoneNumber;
                oldUser.Address = updatedUser.Address;
                var currentRoles = await userManager.GetRolesAsync(oldUser);
                if (currentRoles.Any())
                {
                    await userManager.RemoveFromRolesAsync(oldUser, currentRoles);
                }
                if (!string.IsNullOrEmpty(updatedUser.Role))
                {
                    await userManager.AddToRoleAsync(oldUser, updatedUser.Role);
                }

                oldUser.PasswordHash = userManager.PasswordHasher.HashPassword(oldUser, updatedUser.UserPassword);
                unitOfWork.UserRepo.Update(oldUser);
                await unitOfWork.UserRepo.SaveChanges();
            }
            return new ApiResponse(200, "User updated successfully.");
        }
        // soft delete :
        public async Task<ApiResponse> SoftDeleteUser(string id)
        {
            var user = await unitOfWork.UserRepo.GetById(id);
            if (user == null || user.IsDeleted)
                return new ApiResponse(404, "User not found .");
            user.IsDeleted = true;
            unitOfWork.UserRepo.Update(user);
            await unitOfWork.UserRepo.SaveChanges();
            return new ApiResponse(200, "User deleted successfully.");
        }

        public async Task<ICollection<UserDto>> GetAllUsersPaginated(int pageNumber, int pageSize)
        {
            var users = await unitOfWork.UserRepo.GetAllPaginated(pageNumber, pageSize);
            var usersDto = mapper.Map<List<UserDto>>(users);
            for (int i = 0; i < users.Count; i++)
            {
                var roles = await userManager.GetRolesAsync(users[i]);
                usersDto[i].Role = roles.FirstOrDefault();
            }
            return usersDto;
        }

        public async Task<int> GetPagesCount(int pageSize)
        {
            return await unitOfWork.UserRepo.GetPagesCount(pageSize);
        }

    }
}
