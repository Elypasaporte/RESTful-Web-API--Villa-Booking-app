﻿using AutoMapper;
using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Logging;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.Dto;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace MagicVilla_VillaAPI.Controllers.v1
{
    //[Route("api/[controller")] - difficult to maintain 
    [Route("api/v{version:apiVersion}/VillaAPI")]
    [ApiController]
    [ApiVersion("1.0")]


    public class VillaAPIController : ControllerBase
    {

        // private readonly ApplicationDbContext _db;---> Using ApplicationDbContext

        protected APIResponse _response;
        private readonly IVillaRepository _dbVilla;
        private readonly IMapper _mapper;

        //private readonly ILogging _logger;
        //private readonly ILogger<VillaAPIController> _logger;  | --->> this is the built-in logger
        // public VillaAPIController(ILogging logger)            |
        //{                                                      |
        // _logger = logger;                                     |
        // }                                                     |
        // public VillaAPIController(ApplicationDbContext db,IMapper mapper) | ---> Using ApplicationDbContext
        public VillaAPIController(IVillaRepository dbVilla, IMapper mapper)
        {
            _dbVilla = dbVilla;
            _mapper = mapper;
            _response = new();
        }


        [HttpGet]
        [ResponseCache(CacheProfileName = "Default30")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]

        //public async Task<ActionResult<IEnumerable<VillaDTO>>> GetVillas() // No using API Response
        public async Task<ActionResult<APIResponse>> GetVillas([FromQuery(Name = "filterOccupancy")]int?  occupancy,
            [FromQuery]string? search, int pageSize=0, int pageNumber=1)
        {
            try
            {
                //_logger.Log("Getting all Villas",""); ---> customizing logger in the console
                IEnumerable<Villa> villaList;

                if (occupancy > 0)
                {
                    villaList = await _dbVilla.GetAllAsync(u => u.Occupancy == occupancy, pageSize:pageSize,pageNumber:pageNumber);
                }
                else
                {
                    villaList = await _dbVilla.GetAllAsync(pageSize: pageSize, pageNumber: pageNumber);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    villaList = villaList.Where(u=> u.Name.ToLower().Contains(search));
                }
                Pagination pagination = new() { PageNumber= pageNumber, PageSize = pageSize };
                Response.Headers.Add("X-pagination",JsonSerializer.Serialize(pagination));
                _response.Result = _mapper.Map<List<VillaDTO>>(villaList);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.Message };
            }
            return _response;
        }

        [HttpGet("{id:int}", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ResponseCache(Duration = 30)]
        //[ResponseCache(Location = ResponseCacheLocation.None)]

        //[ProducesResponseType(200)]    | not      | 
        //[ProducesResponseType(400)] -> | very     |
        //[ProducesResponseType(404)]    | detailed |
        public async Task<ActionResult<APIResponse>> GetVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    //_logger.Log("Get villa Error with ID" + id, "error");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess= false;
                    return BadRequest(_response);
                }
                var villa = await _dbVilla.GetAsync(u => u.Id == id);

                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<VillaDTO>(villa);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.Message };
            }
            return _response;

        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<ActionResult<APIResponse>> CreateVilla([FromBody] VillaCreateDTO createDTO)
        {
            //if (!ModelState.IsValid)           |
            //{                                  | ==\ When "[API Controller]" is not present (line 11)
            //    return BadRequest(ModelState); | ==/ or when having a custom validation.
            //}                                  |

            try
            {

                if (await _dbVilla.GetAsync(u => u.Name.ToLower() == createDTO.Name.ToLower()) != null)
                {
                    ModelState.AddModelError("ErrorMessages", "Villa already Exist"); // ("FirstOrDefaultAsyncustomError","Villa already Exist.") --> it is still Okay.
                    return BadRequest(ModelState);
                }
                if (createDTO == null)
                {
                    return BadRequest(createDTO);
                }
                //if (villaDTO.Id > 0)
                //{
                //    return StatusCode(StatusCodes.Status500InternalServerError);---> not needed anymore for separate DTO.
                //}
                //villaDTO.Id = VillaStore.villaList.OrderByDescending(u => u.Id).FirstOrDefault().Id + 1; |====> adding IDs.before entityframwork

                Villa villa = _mapper.Map<Villa>(createDTO);


                //Villa model = new ()                // This block is from entityframework only if automapper is not installed//
                //{   
                //    Amenity = createDTO.Amenity,
                //    Details = createDTO.Details,
                //    //Id = villaDTO.Id,
                //    ImageUrl=createDTO.ImageUrl,
                //    Name=createDTO.Name,
                //    Occupancy=createDTO.Occupancy,
                //    Rate=createDTO.Rate,
                //    Sqft=createDTO.Sqft,

                //};
                await _dbVilla.CreateAsync(villa);
                _response.Result = _mapper.Map<VillaDTO>(villa);
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtRoute("GetVilla", new { id = villa.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.Message };
            }
            return _response;
        }


        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpDelete("{id:int}", Name = "DeleteVilla")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<APIResponse>> DeleteVilla(int id) // public ActionResult<VillaDTO> can also be used
        {
            try
            {
                if (id == 0)
                {
                    return BadRequest();
                    
                }
                var villa = await _dbVilla.GetAsync(u => u.Id == id);

                if (villa == null)
                {
                    return NotFound();
                    
                }
                await _dbVilla.RemoveAsync(villa);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.Message };
            }
            return _response;

        }
        [Authorize(Roles = "admin")]
        [HttpPut("{id:int}", Name = "UpdateVilla")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        public async Task<ActionResult<APIResponse>> UpdateVilla(int id, [FromBody] VillaUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || id != updateDTO.Id)
                {
                    return BadRequest();
                }
                //var villa = VillaStore.villaList.FirstOrDefault(u=> u.Id == id);
                //villa.Name = villaDTO.Name;
                //villa.Sqft  = villaDTO.Sqft;
                //villa.Occupancy= villaDTO.Occupancy;

                Villa model = _mapper.Map<Villa>(updateDTO);

                //Villa model = new()                // This block is from entityframework //
                //{
                //    Amenity = updateDTO.Amenity,
                //    Details = updateDTO.Details,
                //    Id = updateDTO.Id,
                //    ImageUrl = updateDTO.ImageUrl,
                //    Name = updateDTO.Name,
                //    Occupancy = updateDTO.Occupancy,
                //    Rate = updateDTO.Rate,
                //    Sqft = updateDTO.Sqft,

                //};
                await _dbVilla.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.Message };
            }
            return _response;

        }

        [HttpPatch("{id:int}", Name = "UpdatePartialVilla")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]

        public async Task<IActionResult> UpdatePartialVilla(int id, JsonPatchDocument<VillaUpdateDTO> patchDTO)
        {
            if (patchDTO == null || id == 0)
            {
                return BadRequest();
            }
            var villa = await _dbVilla.GetAsync(u => u.Id == id, tracked: false); // AsNoTracking is for efcore not to track the id.

            //villa.Name = "new name";  // automatically updates the name.
            // _db.SaveChanges();        // This is another way of how EFcore works!!! (but we don't want EFcore to track it)

            VillaUpdateDTO villaDTO = _mapper.Map<VillaUpdateDTO>(villa);

            //VillaUpdateDTO villaDTO = new()                // This block is from entityframework //
            //{
            //    Amenity = villa.Amenity,
            //    Details = villa.Details,
            //    Id = villa.Id,
            //    ImageUrl = villa.ImageUrl,
            //    Name = villa.Name,
            //    Occupancy = villa.Occupancy,
            //    Rate = villa.Rate,
            //    Sqft = villa.Sqft,

            //};

            if (villa == null)
            {
                return BadRequest();
            }

            patchDTO.ApplyTo(villaDTO, ModelState);
            Villa model = _mapper.Map<Villa>(villaDTO);

            await _dbVilla.UpdateAsync(model); //--> After the repository   

            //Villa model = new Villa()             =>   // This block is from entityframework //
            //{
            //    Amenity = villaDTO.Amenity,
            //    Details = villaDTO.Details,
            //    Id = villaDTO.Id,
            //    ImageUrl = villaDTO.ImageUrl,
            //    Name = villaDTO.Name,
            //    Occupancy = villaDTO.Occupancy,
            //    Rate = villaDTO.Rate,
            //    Sqft = villaDTO.Sqft,

            //};
            //_db.Villas.Update(model);   ---> before the repository
            // await _db.SaveChangesAsync();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return NoContent();
        }


    }
}
